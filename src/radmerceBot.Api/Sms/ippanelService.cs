using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using radmerceBot.Infrastructure.Sms;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
namespace radmerceBot.Api.Sms;

public class ippanelService
{
    private readonly string TOKEN;
    private readonly HttpClient _httpClient;

    public ippanelService(string Token, HttpClient? httpClient = null)
    {
        TOKEN = Token;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(ippanelUrls.BaseEndpoint);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")); // we have problem here ...
    }
    public async Task<string> SendSmsAsync(
       string fromNumber,
       string message,
       string mobile)
    {
        var payload = new
        {
            sending_type = "webservice",
            from_number = fromNumber,
            message,
            @params = new
            {
                recipients = new[] { mobile }
            }
        };

        var json = JsonConvert.SerializeObject(payload);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, ippanelUrls.BaseEndpoint + ippanelUrls.SendSms)
        {
            Content = content
        };
        request.Headers.Add("Authorization", TOKEN);
        
        var response = await _httpClient.SendAsync(request);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"IPPanel Error: {responseBody}");

        return responseBody;
    }
}