namespace WebAPI.Endpoints.HybridAPI;

/// <summary>
/// Базовый класс для «гибридных» эндпоинтов: методы класса выглядят как обычные
/// Minimal API-обработчики, но сервисы (<c>ICommand</c>, валидаторы и т.п.)
/// резолвятся из DI автоматически, а наружу (в Swagger / OpenAPI) выставляются
/// только DTO-параметры.
/// </summary>
public abstract class EndpointBase
{
    public virtual void MapEndpoint(IEndpointRouteBuilder app)
    {
        var type = GetType();
        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        var basePath = routeAttr?.Template ?? "";

        // Группа с префиксом (но авторизацию НЕ применяем на группе, будем добавлять отдельно к каждому методу)
        var groupBuilder = app.MapGroup(basePath);

        // Тэги Swagger (если есть атрибут Tags)
        var tagsAttr = type.GetCustomAttribute<TagsAttribute>();
        if (tagsAttr != null && tagsAttr.Tags.Any())
            groupBuilder.WithTags(tagsAttr.Tags.ToArray());

        // Собираем классовые атрибуты авторизации (для методов, у которых нет своих)
        var classAuthAttrs = type.GetCustomAttributes<AuthorizeAttribute>().ToList();
        var classAllowAnonymous = type.GetCustomAttribute<AllowAnonymousAttribute>() != null;

        // Методы класса
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType != typeof(object) && !m.IsSpecialName);

        foreach (var method in methods)
        {
            var httpAttr = method.GetCustomAttributes()
                .FirstOrDefault(a => a is HttpMethodAttribute) as HttpMethodAttribute;
            if (httpAttr == null) continue;

            var httpMethod = httpAttr switch
            {
                HttpGetAttribute => "GET",
                HttpPostAttribute => "POST",
                HttpPutAttribute => "PUT",
                HttpDeleteAttribute => "DELETE",
                HttpPatchAttribute => "PATCH",
                _ => "GET"
            };

            // Путь: внутри MapGroup всегда относительный — префикс группы применяется автоматически.
            // Пустой шаблон заменяем на "/", иначе роутинг ругается.
            var methodTemplate = string.IsNullOrEmpty(httpAttr.Template)
                ? "/"
                : httpAttr.Template;

            // Создаём делегат-обработчик и метаданные для OpenAPI
            var (handler, metadata) = CreateHandler(method, app.ServiceProvider);

            // Регистрируем эндпоинт
            var builder = groupBuilder.MapMethods(methodTemplate, new[] { httpMethod }, handler);

            // Приклеиваем метаданные (в т.ч. ParameterBindingMetadata для Swagger)
            if (metadata.Length > 0)
                builder.WithMetadata(metadata);

            // Авторизация – применяется на уровне метода, переопределяя классовые требования
            ApplyAuthorization(builder, method, classAuthAttrs, classAllowAnonymous);
        }
    }

    /// <summary>
    /// Применяет авторизацию к конкретному методу.
    /// Если у метода есть свои атрибуты [Authorize] или [AllowAnonymous] – используем их,
    /// иначе применяем классовые.
    /// </summary>
    private static void ApplyAuthorization(
        IEndpointConventionBuilder builder,
        MethodInfo method,
        List<AuthorizeAttribute> classAuthAttrs,
        bool classAllowAnonymous)
    {
        var methodAuthAttrs = method.GetCustomAttributes<AuthorizeAttribute>().ToList();
        var methodAllowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null;

        // Если метод помечен [AllowAnonymous] – отключаем авторизацию
        if (methodAllowAnonymous)
        {
            builder.AllowAnonymous();
            return;
        }

        // Если у метода есть свои [Authorize] – применяем только их
        if (methodAuthAttrs.Any())
        {
            var roles = methodAuthAttrs
                .SelectMany(a => a.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .ToArray();

            if (roles.Any())
                builder.RequireAuthorization(policy => policy.RequireRole(roles));
            else
                builder.RequireAuthorization(); // просто RequireAuthorization (любой аутентифицированный)
            return;
        }

        // Если у метода нет своих атрибутов – применяем классовые
        if (classAllowAnonymous)
        {
            builder.AllowAnonymous();
        }
        else if (classAuthAttrs.Any())
        {
            var roles = classAuthAttrs
                .SelectMany(a => a.Roles?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct()
                .ToArray();

            if (roles.Any())
                builder.RequireAuthorization(policy => policy.RequireRole(roles));
            else
                builder.RequireAuthorization();
        }
        // Если нет ни классовых, ни методных атрибутов – авторизация не добавляется (AllowAnonymous по умолчанию)
    }

    /// <summary>
    /// Создаёт делегат-обработчик для метода эндпоинта.
    ///
    /// Использует <see cref="RequestDelegateFactory"/> — тот же механизм, что и Minimal API
    /// внутри MapGet/MapPost. Он сам:
    ///   · биндит DTO-параметры из body/route/query,
    ///   · резолвит сервисы (ICommand и пр.) из DI,
    ///   · подставляет HttpContext, CancellationToken, IServiceProvider,
    ///   · генерирует EndpointMetadata, по которой Swagger рисует параметры и схемы.
    ///
    /// ВАЖНО: RequestDelegateFactory.Create НЕ кладёт MethodInfo в EndpointMetadata,
    /// а MinimalApiActionDescriptorProvider без MethodInfo отбрасывает эндпоинт,
    /// из-за чего Swagger показывает «No operations defined in spec!».
    /// Поэтому MethodInfo добавляем вручную.
    /// </summary>
    private static (RequestDelegate Handler, object[] Metadata) CreateHandler(MethodInfo method, IServiceProvider services)
    {
        var options = new RequestDelegateFactoryOptions
        {
            ServiceProvider = services,
            ThrowOnBadRequest = true // todo false
        };

        var result = RequestDelegateFactory.Create(
            method,
            targetFactory: httpContext =>
                ActivatorUtilities.CreateInstance(
                    httpContext.RequestServices,
                    method.DeclaringType!),
            options);

        // MethodInfo — первым, чтобы Swashbuckle гарантированно его нашёл.
        var metadata = new List<object>(result.EndpointMetadata.Count + 1) { method };
        metadata.AddRange(result.EndpointMetadata);

        return (result.RequestDelegate, metadata.ToArray());
    }
}