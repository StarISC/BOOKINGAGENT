using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BookingAgent.Domain.Config;
using BookingAgent.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingAgent.App.Services;

public interface ISailingListService
{
    Task<IReadOnlyList<SailingOptionResult>> GetSailingListAsync(CruisePriceCriteria criteria);
}

public sealed class SailingListService : ISailingListService
{
    private readonly HttpClient _httpClient;
    private readonly RoyalCaribbeanApiOptions _options;
    private readonly ILogger<SailingListService> _logger;

    public SailingListService(HttpClient httpClient, IOptions<RoyalCaribbeanApiOptions> options, ILogger<SailingListService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<SailingOptionResult>> GetSailingListAsync(CruisePriceCriteria criteria)
    {
        if (_options.UseStub || string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return Task.FromResult<IReadOnlyList<SailingOptionResult>>(Stub());
        }
        return ExecuteAsync(criteria);
    }

    private async Task<IReadOnlyList<SailingOptionResult>> ExecuteAsync(CruisePriceCriteria criteria)
    {
        var baseUri = _options.BaseUrl.EndsWith("/") ? _options.BaseUrl : $"{_options.BaseUrl}/";
        var endpoint = new Uri($"{baseUri}SailingList");
        var envelope = BuildEnvelope(criteria);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "application/xml")
        };

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
            return ParseResponse(xml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch sailing list; returning stub.");
            return Stub();
        }
    }

    private string BuildEnvelope(CruisePriceCriteria criteria)
    {
        var start = criteria.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var minDuration = "P1N";
        var maxDuration = criteria.DurationNights.HasValue ? $"P{criteria.DurationNights}N" : "P14N";
        var regionCode = string.IsNullOrWhiteSpace(criteria.RegionCode) ? "FAR.E" : criteria.RegionCode;
        var subRegionCode = "FAR";

        var sb = new StringBuilder();
        sb.AppendLine(@"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">");
        sb.AppendLine("   <soap:Body>");
        sb.AppendLine(@"      <getSailingList xmlns=""http://services.rccl.com/Interfaces/SailingList"">");
        sb.AppendLine($@"	<OTA_CruiseSailAvailRQ MaxResponses=""40"" MoreIndicator=""true"" RetransmissionIndicator=""false"" SequenceNmbr=""1"" TimeStamp=""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}"" TransactionIdentifier=""106597"" Version=""1.0"" xmlns=""http://www.opentravel.org/OTA/2003/05/alpha"">");
        sb.AppendLine("		<POS>");
        sb.AppendLine($@"            <Source ISOCurrency=""USD"" TerminalID=""{_options.TerminalId}"">");
        sb.AppendLine($@"                <RequestorID Type=""5"" ID=""{_options.RequestorId}"" ID_Context=""AGENCY_1"" />");
        sb.AppendLine(@"                <BookingChannel Type=""7"">");
        sb.AppendLine($@"                    <CompanyName CompanyShortName=""{_options.CompanyShortName}"" />");
        sb.AppendLine(@"                </BookingChannel>");
        sb.AppendLine(@"            </Source>");
        sb.AppendLine("        </POS>");
        sb.AppendLine("		<GuestCounts>");
        var guestCount = Math.Max(1, criteria.Guests.Count == 0 ? 2 : criteria.Guests.Count);
        for (int i = 0; i < guestCount; i++)
        {
            sb.AppendLine(@"			<GuestCount Quantity=""1""/>");
        }
        sb.AppendLine("		</GuestCounts>");
        sb.AppendLine($@"		<SailingDateRange Start=""{start:yyyy-MM-dd}"" MinDuration=""{minDuration}"" MaxDuration=""{maxDuration}""/>");
        sb.AppendLine(@"		<CruiseLinePrefs>");
        sb.AppendLine($@"			<CruiseLinePref VendorCode=""{_options.VendorCode}"">");
        sb.AppendLine(@"				<SearchQualifiers/>");
        sb.AppendLine(@"			</CruiseLinePref>");
        sb.AppendLine(@"		</CruiseLinePrefs>");
        sb.AppendLine($@"        <RegionPref RegionCode=""{regionCode}"" SubRegionCode=""{subRegionCode}""></RegionPref>");
        sb.AppendLine("	</OTA_CruiseSailAvailRQ>");
        sb.AppendLine("</getSailingList>");
        sb.AppendLine("   </soap:Body>");
        sb.AppendLine("</soap:Envelope>");
        return sb.ToString();
    }

    private IReadOnlyList<SailingOptionResult> ParseResponse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var ns = XNamespace.Get("http://www.opentravel.org/OTA/2003/05/alpha");
            var options = doc.Descendants(ns + "SailingOption");
            return options.Select(opt =>
            {
                var selected = opt.Element(ns + "SelectedSailing");
                var line = selected?.Element(ns + "CruiseLine");
                var pkg = opt.Element(ns + "InclusivePackageOption");
                return new SailingOptionResult
                {
                    CruisePackageCode = pkg?.Attribute("CruisePackageCode")?.Value,
                    ShipCode = line?.Attribute("ShipCode")?.Value,
                    VendorCode = line?.Attribute("VendorCode")?.Value,
                    RegionCode = selected?.Element(ns + "Region")?.Attribute("RegionCode")?.Value ?? selected?.Attribute("ListOfSailingDescriptionCode")?.Value,
                    SubRegionCode = selected?.Element(ns + "Region")?.Attribute("SubRegionCode")?.Value,
                    DeparturePort = selected?.Element(ns + "DeparturePort")?.Attribute("LocationCode")?.Value,
                    ArrivalPort = selected?.Element(ns + "ArrivalPort")?.Attribute("LocationCode")?.Value,
                    StartDate = ParseDate(selected?.Attribute("Start")?.Value),
                    Duration = selected?.Attribute("Duration")?.Value
                };
            }).Where(r => r is not null).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse SailingList response.");
            return Stub();
        }
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (DateOnly.TryParse(value, out var d))
        {
            return d;
        }
        return null;
    }

    private static IReadOnlyList<SailingOptionResult> Stub() => new List<SailingOptionResult>
    {
        new()
        {
            CruisePackageCode = "OV03I198",
            ShipCode = "OV",
            VendorCode = "RCC",
            RegionCode = "FAR.E",
            SubRegionCode = "FAR",
            DeparturePort = "SIN",
            ArrivalPort = "SIN",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Duration = "P3N"
        }
    };
}
