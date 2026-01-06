using Gateway.Api.Services.Interfaces;

namespace Gateway.Api.Services;

public sealed class ProxyService(IHttpClientFactory httpClientFactory, ILogger<ProxyService> logger) : IProxyService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ProxyService> _logger = logger;

    public async Task ProxyRequestAsync(HttpContext context, string serviceName)
    {
        var client = _httpClientFactory.CreateClient(serviceName);

        var targetUri = new Uri(client.BaseAddress!, context.Request.Path + context.Request.QueryString);

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = targetUri
        };

        foreach (var header in context.Request.Headers)
        {
            if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                continue;

            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        if (context.Request.ContentLength > 0)
        {
            var streamContent = new StreamContent(context.Request.Body);
            if (context.Request.ContentType != null)
            {
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
            }
            requestMessage.Content = streamContent;
        }

        try
        {
            var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            context.Response.StatusCode = (int)responseMessage.StatusCode;

            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            context.Response.Headers.Remove("transfer-encoding");

            await responseMessage.Content.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {ServiceName}", serviceName);
            context.Response.StatusCode = 502;
            await context.Response.WriteAsJsonAsync(new { error = "SERVICE_UNAVAILABLE", message = $"Service {serviceName} is unavailable" });
        }
    }
}