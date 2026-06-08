namespace WebAPI.Endpoints.Service;

public static class EndpointDiscoveryExtensions
{
    public static IEndpointRouteBuilder MapDiscoveredEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseEndpoint)) && !t.IsAbstract);

        foreach (var endpointType in endpointTypes)
        {
            // Используем DI для создания экземпляра
            var instance = ActivatorUtilities.CreateInstance(endpoints.ServiceProvider, endpointType);

            var methods = endpointType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsPublic && !m.IsSpecialName && m.GetCustomAttribute<HttpMethodAttribute>() != null);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<HttpMethodAttribute>()!;
                var route = string.IsNullOrEmpty(attr.Template)
                    ? $"/api/{endpointType.Name.Replace("Endpoints", "").ToLower()}"
                    : attr.Template;
                var httpMethod = attr.HttpMethods.First();

                endpoints.MapMethods(route, [httpMethod], CreateHandler(endpointType, method, instance));
            }
        }

        return endpoints;
    }

    private static RequestDelegate CreateHandler(Type endpointType, MethodInfo method, object instance)
    {
        return async context =>
        {
            try
            {
                var parameters = method.GetParameters();
                var args = new List<object>();
                var cancellationToken = context.RequestAborted;

                foreach (var param in parameters)
                {
                    var value = await ResolveParameter(param, context, cancellationToken);
                    args.Add(value!);
                }

                var result = method.Invoke(instance, args.ToArray());

                if (result is Task task)
                {
                    await task;
                    result = task.GetType().GetProperty("Result")?.GetValue(task);
                }

                if (result is IActionResult actionResult)
                {
                    await actionResult.ExecuteResultAsync(new ActionContext(
                        context,
                        context.GetRouteData(),
                        new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()));
                }
                else if (result != null)
                {
                    await context.Response.WriteAsJsonAsync(result);
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        };
    }

    private static async ValueTask<object?> ResolveParameter(
        ParameterInfo param,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (param.ParameterType == typeof(CancellationToken))
            return cancellationToken;

        if (param.ParameterType == typeof(Guid) &&
            context.Request.RouteValues.TryGetValue("id", out var routeId) &&
            routeId != null)
            return Guid.Parse(routeId.ToString()!);

        if (param.GetCustomAttribute<FromBodyAttribute>() != null)
            return await context.Request.ReadFromJsonAsync(param.ParameterType, cancellationToken);

        if (param.ParameterType == typeof(EmptyRequest))
            return new EmptyRequest();

        if (param.ParameterType.GetConstructor(Type.EmptyTypes) != null)
            return Activator.CreateInstance(param.ParameterType);

        return null;
    }
}
