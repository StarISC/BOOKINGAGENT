using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BookingAgent.Domain.Config;

namespace BookingAgent.App.Services;

public interface ILoginApiService
{
    Task<LoginApiResult> LoginAsync();
}

public sealed class LoginApiService : ILoginApiService
{
    private readonly HttpClient _httpClient;
    private readonly RoyalCaribbeanApiOptions _options;
    private readonly ILogger<LoginApiService> _logger;

    public LoginApiService(HttpClient httpClient, IOptions<RoyalCaribbeanApiOptions> options, ILogger<LoginApiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LoginApiResult> LoginAsync()
    {
        var endpoint = new Uri(new Uri(_options.BaseUrl.TrimEnd('/')), "Login");
        var envelope = BuildEnvelope();
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "application/xml")
        };

        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            var bytes = Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }

        try
        {
            var response = await _httpClient.SendAsync(request);
            var xml = await response.Content.ReadAsStringAsync();
            return new LoginApiResult
            {
                StatusCode = (int)response.StatusCode,
                RawResponse = xml,
                IsSuccess = response.IsSuccessStatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login API call failed");
            return new LoginApiResult
            {
                StatusCode = 0,
                RawResponse = ex.Message,
                IsSuccess = false
            };
        }
    }

    private string BuildEnvelope()
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">");
        sb.AppendLine("<soap:Body>");
        sb.AppendLine(@"  <login xmlns=""http://services.rccl.com/Interfaces/Login"">");
        sb.AppendLine($@"    <RCL_CruiseLoginRQ TimeStamp=""{DateTime.UtcNow:O}"" Target=""Test"" Version=""1"" TransactionIdentifier=""123456"" SequenceNmbr=""1"" TransactionStatusCode=""Start"" RetransmissionIndicator=""false"" xmlns=""http://www.opentravel.org/OTA/2003/05/alpha"">");
        sb.AppendLine("      <POS>");
        for (int i = 0; i < 3; i++)
        {
            sb.AppendLine($@"        <Source ISOCurrency=""USD"" TerminalID=""{_options.TerminalId}"">");
            sb.AppendLine($@"          <RequestorID Type=""5"" ID=""{_options.RequestorId}""/>");
            sb.AppendLine(@"          <BookingChannel Type=""7"">");
            sb.AppendLine($@"            <CompanyName CompanyShortName=""{_options.CompanyShortName}""/>");
            sb.AppendLine(@"          </BookingChannel>");
            sb.AppendLine(@"        </Source>");
        }
        sb.AppendLine("      </POS>");
        sb.AppendLine("    </RCL_CruiseLoginRQ>");
        sb.AppendLine("  </login>");
        sb.AppendLine("</soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }
}

public sealed class LoginApiResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string RawResponse { get; set; } = string.Empty;
}
