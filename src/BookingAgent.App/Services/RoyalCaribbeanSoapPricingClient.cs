using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BookingAgent.Domain.Config;
using BookingAgent.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingAgent.App.Services;

/// <summary>
/// Skeleton SOAP client for Royal Caribbean pricing.
/// Currently returns sample data; replace the TODO section with actual SOAP call/parse.
/// </summary>
public sealed class RoyalCaribbeanSoapPricingClient : ICruisePricingService
{
    private readonly HttpClient _httpClient;
    private readonly RoyalCaribbeanApiOptions _options;
    private readonly ILogger<RoyalCaribbeanSoapPricingClient> _logger;

    public RoyalCaribbeanSoapPricingClient(
        HttpClient httpClient,
        IOptions<RoyalCaribbeanApiOptions> options,
        ILogger<RoyalCaribbeanSoapPricingClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<CruisePricingResult> GetLatestPricingAsync()
    {
        // TODO: Build OTA_CruisePriceBookingRQ SOAP envelope using lookups and user input.
        // For now, return the sample until the real integration is wired.
        return Task.FromResult(SampleCruisePricingService.BuildSample());
    }

    public Task<CruisePricingResult> GetPriceAsync(CruisePriceCriteria criteria)
    {
        return ExecutePricingAsync(criteria);
    }

    private async Task<CruisePricingResult> ExecutePricingAsync(CruisePriceCriteria criteria)
    {
        if (_options.UseStub || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogInformation("Using stub pricing (UseStub=true or BaseUrl missing).");
            return SampleCruisePricingService.BuildSample();
        }

        var endpoint = BuildEndpoint();
        var envelope = RoyalCaribbeanPricingRequestBuilder.Build(criteria, _options.CompanyShortName, _options.VendorCode);
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
        };
        if (!string.IsNullOrWhiteSpace(_options.SoapAction))
        {
            request.Headers.Add("SOAPAction", _options.SoapAction);
        }

        if (!string.IsNullOrEmpty(_options.Username) && !string.IsNullOrEmpty(_options.Password))
        {
            var raw = $"{_options.Username}:{_options.Password}";
            var bytes = Encoding.UTF8.GetBytes(raw);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
        }

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var xml = await response.Content.ReadAsStringAsync();
            return RoyalCaribbeanPricingResponseParser.Parse(xml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pricing request failed; returning sample.");
            return SampleCruisePricingService.BuildSample();
        }
    }

    private string BuildSoapEnvelope()
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ota=""http://www.opentravel.org/OTA/2003/05/alpha"">");
        sb.AppendLine("<soapenv:Header/>");
        sb.AppendLine("<soapenv:Body>");
        sb.AppendLine("<ota:OTA_CruisePriceBookingRQ Version=\"1.0\">");
        sb.AppendLine("<ota:POS>");
        sb.AppendLine($"  <ota:Source ISOCurrency=\"USD\" />");
        sb.AppendLine("</ota:POS>");
        // Add SailingInfo, InclusivePackageOption, SelectedCategory, GuestDetails, Promotions...
        sb.AppendLine("</ota:OTA_CruisePriceBookingRQ>");
        sb.AppendLine("</soapenv:Body>");
        sb.AppendLine("</soapenv:Envelope>");
        return sb.ToString();
    }

    private Uri BuildEndpoint()
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("RoyalCaribbeanApi:BaseUrl is not configured.");
        }
        var path = string.IsNullOrWhiteSpace(_options.OperationPath) ? "BookingPrice" : _options.OperationPath.TrimStart('/');
        var baseUri = _options.BaseUrl.EndsWith("/") ? _options.BaseUrl : $"{_options.BaseUrl}/";
        return new Uri($"{baseUri}{path}");
    }
}
