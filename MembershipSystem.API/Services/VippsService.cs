using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MembershipSystem.API.Data;
using MembershipSystem.API.Models;

namespace MembershipSystem.API.Services
{
    public class VippsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public VippsService(
            HttpClient httpClient,
            IConfiguration configuration,
            AppDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
        }

        public async Task<string> CreatePaymentLink(int memberId)
        {
            var currentYear = DateTime.Today.Year + 1;

            var alreadyPaid = _context.Payments.Any(p =>
                p.MemberId == memberId &&
                p.PaymentYear == currentYear &&
                p.PaymentDate != null);

            if (alreadyPaid)
                return "ALREADY_PAID";

            var token = await GetAccessToken();

            var accessToken = JsonDocument.Parse(token)
                .RootElement
                .GetProperty("access_token")
                .GetString();

            var reference = $"member-{memberId}-{Guid.NewGuid()}";

            var payment = new Payment
{
    MemberId = memberId,
    PaymentYear = DateTime.Today.Year + 1,
    PaymentReference = reference,
    Amount = 300,
    PaymentDate = null
};

_context.Payments.Add(payment);
_context.SaveChanges();

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_configuration["Vipps:BaseUrl"]}/epayment/v1/payments");

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            request.Headers.Add(
                "Ocp-Apim-Subscription-Key",
                _configuration["Vipps:SubscriptionKey"]);

            request.Headers.Add(
                "Merchant-Serial-Number",
                _configuration["Vipps:MerchantSerialNumber"]);

            request.Headers.Add(
                "Idempotency-Key",
                Guid.NewGuid().ToString());

var returnUrl = $"https://membership-system-api.azurewebsites.net/api/members/payment-result?memberId={memberId}&reference={reference}";

            var json = $@"
{{
  ""amount"": {{
      ""value"":30000,
      ""currency"":""NOK""
  }},
  ""paymentMethod"": {{
      ""type"":""WALLET""
  }},
  ""reference"":""{reference}"",
  ""returnUrl"":""{returnUrl}"",
  ""userFlow"":""WEB_REDIRECT""
}}";

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Bunu ekle
Console.WriteLine("PAYMENT CREATE RESPONSE:");
Console.WriteLine(content);

            var jsonDoc = JsonDocument.Parse(content);

            return jsonDoc.RootElement
                .GetProperty("redirectUrl")
                .GetString();
        }

        private async Task<string> GetAccessToken()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_configuration["Vipps:BaseUrl"]}/accesstoken/get");

            request.Headers.Add(
                "client_id",
                _configuration["Vipps:ClientId"]);

            request.Headers.Add(
                "client_secret",
                _configuration["Vipps:ClientSecret"]);

            request.Headers.Add(
                "Ocp-Apim-Subscription-Key",
                _configuration["Vipps:SubscriptionKey"]);

            var response = await _httpClient.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

public async Task<string> GetPaymentStatus(string reference)
{
    try
    {
        // 1️⃣ Token al
        var tokenResponse = await GetAccessToken();

        var tokenJson = JsonDocument.Parse(tokenResponse);

        var accessToken = tokenJson.RootElement
            .GetProperty("access_token")
            .GetString();

        // 2️⃣ Doğru endpoint (details önemli!)
        var url = $"{_configuration["Vipps:BaseUrl"]}/epayment/v1/payments/{reference}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        request.Headers.Add("Ocp-Apim-Subscription-Key",
            _configuration["Vipps:SubscriptionKey"]);

        request.Headers.Add("Merchant-Serial-Number",
            _configuration["Vipps:MerchantSerialNumber"]);

        // 3️⃣ Request gönder
        var response = await _httpClient.SendAsync(request);

        var content = await response.Content.ReadAsStringAsync();

        // 🔥 DEBUG (terminalde göreceksin)
        Console.WriteLine("VIPPS RESPONSE:");
        Console.WriteLine(content);

        if (!response.IsSuccessStatusCode)
            return null;

        // 4️⃣ JSON parse
        var json = JsonDocument.Parse(content);

        Console.WriteLine("FULL VIPPS JSON:");
        Console.WriteLine(json.RootElement.ToString());

        // 5️⃣ Farklı ihtimaller

        // direkt state
        if (json.RootElement.TryGetProperty("state", out var state))
        {
            return state.GetString();
        }

        // aggregate.state
        if (json.RootElement.TryGetProperty("aggregate", out var aggregate) &&
            aggregate.TryGetProperty("state", out var aggState))
        {
            return aggState.GetString();
        }

        // payments[0].state
        if (json.RootElement.TryGetProperty("payments", out var payments) &&
            payments.ValueKind == JsonValueKind.Array &&
            payments.GetArrayLength() > 0)
        {
            var first = payments[0];

            if (first.TryGetProperty("state", out var paymentState))
            {
                return paymentState.GetString();
            }
        }

        // 🔥 fallback (string scan)
        var raw = content.ToLower();

        if (raw.Contains("captured"))
            return "CAPTURED";

        if (raw.Contains("authorized"))
            return "AUTHORIZED";

        if (raw.Contains("created"))
            return "CREATED";

        return null;
    }
    catch (Exception ex)
    {
        Console.WriteLine("VIPPS ERROR:");
        Console.WriteLine(ex.Message);
        return null;
    }
}
    }
}