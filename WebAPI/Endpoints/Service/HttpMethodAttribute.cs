namespace WebAPI.Endpoints.Service;

[AttributeUsage(AttributeTargets.Method)]
public class HttpMethodAttribute : Attribute
{
    public string Template { get; }
    public string[] HttpMethods { get; }

    public HttpMethodAttribute(string template, params string[] httpMethods)
    {
        Template = template;
        HttpMethods = httpMethods;
    }
}