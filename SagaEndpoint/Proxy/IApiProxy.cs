using SagaEndpoint.Models;
using System.Threading.Tasks;

namespace SagaEndpoint.Proxy
{
    public interface IApiProxy
    {
        Task<ApiResponse> MakeCallbackRequest(string url, string token);
        Task<ApiResponse> MakeInitialRequest(string url, string requestId);
    }
}