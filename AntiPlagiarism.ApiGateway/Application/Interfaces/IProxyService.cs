namespace AntiPlagiarism.ApiGateway.Application.Interfaces
{
    public interface IProxyService
    {
        Task<HttpResponseMessage> ProxyRequestAsync(HttpRequest request, string targetService, string targetPath);
    }
}