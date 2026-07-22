namespace WebAPI.Endpoints.HybridAPI;

public abstract class EndpointBase
{
    public virtual void MapEndpoint(IEndpointRouteBuilder app)
    {
        var type = GetType();
        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        var basePath = routeAttr?.Template ?? "";

        // 1. Группа с префиксом (но авторизацию НЕ применяем на группе, будем добавлять отдельно к каждому методу)
        var groupBuilder = app.MapGroup(basePath);

        // 2. Тэги Swagger (если есть атрибут Tags)
        var tagsAttr = type.GetCustomAttribute<TagsAttribute>();
        if (tagsAttr != null && tagsAttr.Tags.Any())
            groupBuilder.WithTags(tagsAttr.Tags.ToArray());

        // 3. Собираем классовые атрибуты авторизации (для методов, у которых нет своих)
        var classAuthAttrs = type.GetCustomAttributes<AuthorizeAttribute>().ToList();
        var classAllowAnonymous = type.GetCustomAttribute<AllowAnonymousAttribute>() != null;

        // 4. Методы класса
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

            // Путь: если шаблон начинается с "/" – абсолютный, иначе относительный внутри группы
            var methodTemplate = httpAttr.Template ?? "";
            var fullPath = methodTemplate.StartsWith("/")
                ? methodTemplate
                : (string.IsNullOrEmpty(basePath) ? methodTemplate : $"{basePath}/{methodTemplate}".TrimEnd('/'));

            // Создаём делегат-обработчик
            var handler = CreateHandler(method);

            // Регистрируем эндпоинт
            var builder = groupBuilder.MapMethods(fullPath, new[] { httpMethod }, handler);

            // 5. Авторизация – применяется на уровне метода, переопределяя классовые требования
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
    /// Создаёт делегат для вызова метода экземпляра с поддержкой атрибутов привязки:
    /// [FromBody], [FromQuery], [FromRoute], [FromServices], CancellationToken, IServiceProvider,
    /// а также автоматическое разрешение сервисов из DI для параметров, не помеченных атрибутами.
    /// </summary>
    private static Func<HttpContext, Task<IResult>> CreateHandler(MethodInfo method)
    {
        return async (HttpContext ctx) =>
        {
            var endpointType = method.DeclaringType!;
            var instance = ActivatorUtilities.CreateInstance(ctx.RequestServices, endpointType);

            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];

            bool isPostOrPut = method.GetCustomAttribute<HttpPostAttribute>() != null ||
                               method.GetCustomAttribute<HttpPutAttribute>() != null;

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var paramType = param.ParameterType;
                var paramName = param.Name!;

                // 1. CancellationToken
                if (paramType == typeof(CancellationToken))
                {
                    args[i] = ctx.RequestAborted;
                    continue;
                }

                // 2. IServiceProvider
                if (paramType == typeof(IServiceProvider))
                {
                    args[i] = ctx.RequestServices;
                    continue;
                }

                // 3. [FromServices]
                if (param.GetCustomAttribute<FromServicesAttribute>() != null)
                {
                    args[i] = ctx.RequestServices.GetRequiredService(paramType);
                    continue;
                }

                // 4. Автоматическое определение сервиса (если тип зарегистрирован в DI)
                var serviceFromDi = ctx.RequestServices.GetService(paramType);
                if (serviceFromDi != null)
                {
                    args[i] = serviceFromDi;
                    continue;
                }

                // 5. [FromBody]
                if (param.GetCustomAttribute<FromBodyAttribute>() != null)
                {
                    args[i] = await ctx.Request.ReadFromJsonAsync(paramType, ctx.RequestAborted);
                    continue;
                }

                // 6. [FromRoute]
                var fromRouteAttr = param.GetCustomAttribute<FromRouteAttribute>();
                if (fromRouteAttr != null)
                {
                    var routeKey = fromRouteAttr.Name ?? paramName;
                    if (ctx.Request.RouteValues.TryGetValue(routeKey, out var routeValue))
                    {
                        args[i] = ConvertValue(routeValue, paramType);
                    }
                    else
                    {
                        args[i] = GetDefaultValue(paramType);
                    }
                    continue;
                }

                // 7. [FromQuery]
                var fromQueryAttr = param.GetCustomAttribute<FromQueryAttribute>();
                if (fromQueryAttr != null)
                {
                    var queryKey = fromQueryAttr.Name ?? paramName;
                    if (ctx.Request.Query.TryGetValue(queryKey, out var queryValue))
                    {
                        var stringVal = queryValue.ToString();
                        args[i] = ConvertValue(stringVal, paramType);
                    }
                    else
                    {
                        args[i] = GetDefaultValue(paramType);
                    }
                    continue;
                }

                // 8. Для POST/PUT: если параметр не помечен другими атрибутами,
                //    читаем из тела (предполагаем, что это DTO запроса)
                if (isPostOrPut && !param.GetCustomAttributes().Any(a => a is FromQueryAttribute || a is FromRouteAttribute))
                {
                    args[i] = await ctx.Request.ReadFromJsonAsync(paramType, ctx.RequestAborted);
                    continue;
                }

                // 9. По умолчанию (для GET/DELETE): сначала пробуем маршрут, затем query
                if (ctx.Request.RouteValues.TryGetValue(paramName, out var defaultRouteVal))
                {
                    args[i] = ConvertValue(defaultRouteVal, paramType);
                }
                else if (ctx.Request.Query.TryGetValue(paramName, out var defaultQueryVal))
                {
                    var stringVal = defaultQueryVal.ToString();
                    args[i] = ConvertValue(stringVal, paramType);
                }
                else
                {
                    args[i] = GetDefaultValue(paramType);
                }
            }

            // Вызов метода
            var result = method.Invoke(instance, args);

            // Обработка результата
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                if (task is Task<IResult> taskResult)
                    return await taskResult;
                throw new InvalidOperationException($"Method {method.Name} returns Task but not Task<IResult>.");
            }
            else if (result is IResult ires)
            {
                return ires;
            }
            else
            {
                throw new InvalidOperationException($"Method {method.Name} does not return IResult or Task<IResult>.");
            }
        };
    }

    /// <summary>
    /// Преобразует значение к целевому типу с поддержкой nullable.
    /// </summary>
    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return GetDefaultValue(targetType);

        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var effectiveType = underlyingType ?? targetType;

        try
        {
            // Если значение уже нужного типа, возвращаем как есть
            if (effectiveType.IsInstanceOfType(value))
                return value;

            // Преобразование через Convert.ChangeType
            var converted = Convert.ChangeType(value, effectiveType);
            return converted;
        }
        catch
        {
            // При ошибке конвертации возвращаем default
            return GetDefaultValue(targetType);
        }
    }

    /// <summary>
    /// Возвращает значение по умолчанию для типа (null для ссылочных, default для значимых).
    /// </summary>
    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}