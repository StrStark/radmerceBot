using Newtonsoft.Json;
using radmerceBot.Infrastructure.Sms;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace radmerceBot.Api.Sms;

public class ippanelService
{
    private readonly string _token;
    private readonly HttpClient _httpClient;

    public ippanelService(string token, HttpClient? httpClient = null)
    {
        _token = token;
        _httpClient = httpClient ?? new HttpClient();

        _httpClient.BaseAddress = new Uri(ippanelUrls.BaseEndpoint);

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> SendSmsAsync(
        string fromNumber,
        string message,
        string mobile)
    {
        Console.WriteLine("Sending Otp Token to :  ");
        var payload = new
        {
            sending_type = "webservice",
            from_number = fromNumber,
            message = message,
            @params = new
            {
                recipients = new[] { $"+{mobile}" }
            }
        };

        var json = JsonConvert.SerializeObject(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, ippanelUrls.BaseEndpoint + ippanelUrls.SendSms)
        {
            Content = content
        };
        Console.WriteLine(ippanelUrls.BaseEndpoint + ippanelUrls.SendSms);
        request.Headers.TryAddWithoutValidation("Authorization", _token);

        var response = await _httpClient.SendAsync(request);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"IPPanel Error: {responseBody}");

        return responseBody;
    }
}
