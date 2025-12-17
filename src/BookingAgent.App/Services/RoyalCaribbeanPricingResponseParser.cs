using System.Linq;
using System.Xml.Linq;
using BookingAgent.Domain.Models;

namespace BookingAgent.App.Services;

/// <summary>
/// Placeholder parser for OTA_CruisePriceBookingRS SOAP responses.
/// TODO: Implement XML parsing to map to CruisePricingResult.
/// </summary>
public static class RoyalCaribbeanPricingResponseParser
{
    public static CruisePricingResult Parse(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            var ns = XNamespace.Get("http://www.opentravel.org/OTA/2003/05/alpha");

            var sailing = doc.Descendants(ns + "SelectedSailing").FirstOrDefault();
            var cruiseLine = sailing?.Element(ns + "CruiseLine");
            var category = doc.Descendants(ns + "SelectedCategory").FirstOrDefault();
            var bookingPrices = doc.Descendants(ns + "BookingPrice").ToList();
            var payments = doc.Descendants(ns + "Payment").ToList();
            var promotions = doc.Descendants(ns + "SelectedPromotions").ToList();

            var result = new CruisePricingResult
            {
                ReservationId = doc.Descendants(ns + "ReservationID").FirstOrDefault()?.Attribute("ID")?.Value,
                SailingInfo = new CruiseSailingInfo
                {
                    SelectedSailing = sailing is null
                        ? null
                        : new SelectedSailing
                        {
                            Start = ParseDate(sailing.Attribute("Start")?.Value),
                            ShipCode = cruiseLine?.Attribute("ShipCode")?.Value,
                            VendorCode = cruiseLine?.Attribute("VendorCode")?.Value,
                            Duration = sailing.Attribute("Duration")?.Value
                        },
                    SelectedCategory = category is null
                        ? null
                        : new BookingSelectedCategory
                        {
                            FareCode = category.Attribute("FareCode")?.Value,
                            PromotionDescription = category.Attribute("PromotionDescription")?.Value,
                            PricedCategoryCode = category.Attribute("PricedCategoryCode")?.Value,
                            BerthedCategoryCode = category.Attribute("BerthedCategoryCode")?.Value
                        }
                },
                BookingPayment = new BookingPayment
                {
                    BookingPrices = bookingPrices.Select(bp => new BookingPrice
                    {
                        Amount = ParseDecimal(bp.Attribute("Amount")?.Value),
                        PriceTypeCode = bp.Attribute("PriceTypeCode")?.Value,
                        RestrictedIndicator = ParseBool(bp.Attribute("RestrictedIndicator")?.Value)
                    }).ToList(),
                    PaymentSchedule = new PaymentSchedule
                    {
                        Payments = payments.Select(p => new Payment
                        {
                            Amount = ParseDecimal(p.Attribute("Amount")?.Value),
                            PaymentNumber = ParseInt(p.Attribute("PaymentNumber")?.Value),
                            DueDate = ParseDate(p.Attribute("DueDate")?.Value)
                        }).ToList()
                    }
                }
            };

            // Guest price details
            var guestPrices = doc.Descendants(ns + "GuestPrice").ToList();
            foreach (var gp in guestPrices)
            {
                var priceInfos = gp.Descendants(ns + "PriceInfo").Select(pi => new GuestPriceInfo
                {
                    Amount = ParseDecimal(pi.Attribute("Amount")?.Value),
                    PriceTypeCode = pi.Attribute("PriceTypeCode")?.Value,
                    PriceComponentCode = pi.Attribute("CodeDetail")?.Value,
                    PricingLevel = pi.Attribute("PricingLevel")?.Value,
                    PricingType = pi.Attribute("PricingType")?.Value,
                    AutoAddedIndicator = ParseBool(pi.Attribute("AutoAddedIndicator")?.Value),
                    SelectedOptionsIndicator = ParseBool(pi.Attribute("SelectedOptionsIndicator")?.Value),
                    MandatoryIndicator = ParseBool(pi.Attribute("MandatoryIndicator")?.Value),
                    ItemizableIndicator = ParseBool(pi.Attribute("ItemizableIndicator")?.Value),
                    NonRefundableType = pi.Attribute("NonRefundableType")?.Value
                }).ToList();

                result.BookingPayment?.GuestPrices.Add(new GuestPrice
                {
                    PriceInfos = priceInfos
                });
            }

            if (promotions.Any() && result.SailingInfo?.SelectedCategory is not null)
            {
                foreach (var promo in promotions)
                {
                    var code = promo.Attribute("PromotionCode")?.Value;
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        result.SailingInfo.SelectedCategory.Promotions.Add(new PromotionDetail
                        {
                            PromotionCode = code
                        });
                    }
                }
            }

            return result;
        }
        catch
        {
            // fallback to sample if parsing fails
            return SampleCruisePricingService.BuildSample();
        }
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (DateOnly.TryParse(value, out var date))
        {
            return date;
        }
        return null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (decimal.TryParse(value, out var d))
        {
            return d;
        }
        return null;
    }

    private static bool? ParseBool(string? value)
    {
        if (bool.TryParse(value, out var b)) return b;
        return null;
    }

    private static int? ParseInt(string? value)
    {
        if (int.TryParse(value, out var i)) return i;
        return null;
    }
}
