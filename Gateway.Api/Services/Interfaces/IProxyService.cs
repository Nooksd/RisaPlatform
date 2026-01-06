namespace Gateway.Api.Services.Interfaces;

public interface IProxyService
{
    Task ProxyRequestAsync(HttpContext context, string serviceName);
}