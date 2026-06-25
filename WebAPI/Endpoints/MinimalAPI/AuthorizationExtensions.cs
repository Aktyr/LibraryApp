namespace WebAPI.Endpoints.MinimalAPI;

public static class AuthorizationExtensions
{
    public static TBuilder RequireRoles<TBuilder>(this TBuilder builder, params UserRole[] roles)
         where TBuilder : IEndpointConventionBuilder
    {
        if (roles == null || roles.Length == 0)
            return builder;

        var roleNames = roles.Select(r => r.ToString()).ToArray();
        return builder.RequireAuthorization(policy => policy.RequireRole(roleNames));
    }

}