using System.Text;
using BookingAgent.Domain.Models;

namespace BookingAgent.App.Services;

/// <summary>
/// Builds OTA_CruisePriceBookingRQ SOAP envelopes from criteria.
/// Simplified but structured per OTA_2007A; extend with guest/promotions when needed.
/// </summary>
public static class RoyalCaribbeanPricingRequestBuilder
{
    public static string Build(CruisePriceCriteria criteria, string companyShortName, string vendorCode = "RCC")
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ota=""http://www.opentravel.org/OTA/2003/05/alpha"">");
        sb.AppendLine("<soapenv:Header/>");
        sb.AppendLine("<soapenv:Body>");
        sb.AppendLine(@"<ota:OTA_CruisePriceBookingRQ Version=""1.0"" TransactionActionCode=""Price"">");
        sb.AppendLine("<ota:POS>");
        sb.AppendLine($@"  <ota:Source ISOCurrency=""{criteria.CurrencyCode ?? "USD"}"">");
        sb.AppendLine($@"    <ota:RequestorID Type=""5"" ID=""{companyShortName}""/>");
        sb.AppendLine($@"    <ota:BookingChannel Type=""7"">");
        sb.AppendLine($@"      <ota:CompanyName Code=""{vendorCode}"" CompanyShortName=""{companyShortName}"" CodeContext=""SABRE"" />");
        sb.AppendLine(@"    </ota:BookingChannel>");
        sb.AppendLine(@"  </ota:Source>");
        sb.AppendLine("</ota:POS>");
        sb.AppendLine("<ota:SailingInfo>");
        if (criteria.StartDate.HasValue)
        {
            var duration = criteria.DurationNights.HasValue ? $" Duration=\"P{criteria.DurationNights}N\"" : string.Empty;
            sb.AppendLine($@"  <ota:SelectedSailing Start=""{criteria.StartDate:yyyy-MM-dd}""{duration}>");
            if (!string.IsNullOrWhiteSpace(criteria.ShipCode))
            {
                sb.AppendLine($@"    <ota:CruiseLine ShipCode=""{criteria.ShipCode}"" VendorCode=""{vendorCode}""/>");
            }
            sb.AppendLine(@"  </ota:SelectedSailing>");
        }
        if (!string.IsNullOrWhiteSpace(criteria.PortCode))
        {
            sb.AppendLine($@"  <ota:DeparturePort LocationCode=""{criteria.PortCode}"" />");
        }
        if (!string.IsNullOrWhiteSpace(criteria.CabinCategoryCode))
        {
            sb.AppendLine($@"  <ota:SelectedCategory FareCode=""{criteria.CabinCategoryCode}"" />");
        }
        sb.AppendLine("</ota:SailingInfo>");
        if (criteria.Guests.Count > 0)
        {
            sb.AppendLine("<ota:ReservationInfo>");
            sb.AppendLine("<ota:GuestDetails>");
            foreach (var guest in criteria.Guests)
            {
                sb.AppendLine(@"  <ota:GuestDetail>");
                sb.AppendLine($@"    <ota:ContactInfo Age=""{guest.Age}"" Nationality=""{guest.Nationality ?? "US"}"" />");
                sb.AppendLine("  </ota:GuestDetail>");
            }
            sb.AppendLine("</ota:GuestDetails>");
            sb.AppendLine("</ota:ReservationInfo>");
        }
        // TODO: add promotions and cabin selection when available.
        sb.AppendLine("</ota:OTA_CruisePriceBookingRQ>");
        sb.AppendLine("</soapenv:Body>");
        sb.AppendLine("</soapenv:Envelope>");
        return sb.ToString();
    }
}
