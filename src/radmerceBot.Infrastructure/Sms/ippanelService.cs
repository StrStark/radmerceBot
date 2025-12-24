using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
namespace radmerceBot.Infrastructure.Sms;

public class ippanelService
{
    private readonly HttpClient _httpClient;

    public ippanelService(string Token, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(ippanelUrls.BaseEndpoint);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _httpClient.DefaultRequestHeaders.Add("Authorization", Token);
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
            message = message,
            @params = new
            {
                recipients = new[] { mobile }
            }
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(ippanelUrls.SendSms, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"IPPanel Error: {responseBody}");

        return responseBody;
    }
}