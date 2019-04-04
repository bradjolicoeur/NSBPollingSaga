using Newtonsoft.Json;
using SagaEndpoint.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SagaEndpoint.Proxy
{
    public class ApiProxy : IApiProxy
    {
        private readonly HttpClient _httpClient;

        public ApiProxy(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse> MakeInitialRequest(string url, string requestId)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept
              .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var c = new StringContent(JsonConvert.SerializeObject(new { RequestId = requestId }));
            c.Headers.ContentType.MediaType =  "application/json";

            var response = await _httpClient.PostAsync(url, c);

            string content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<ApiResponse>(content);

        }

        public async Task<ApiResponse> MakeCallbackRequest(string url, string token)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            var stringTask = await _httpClient.GetStringAsync(url + "/" + token);

            return JsonConvert.DeserializeObject<ApiResponse>(stringTask);
        }

    }
}
