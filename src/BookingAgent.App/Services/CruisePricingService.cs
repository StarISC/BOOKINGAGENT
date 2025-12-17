using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingAgent.Domain.Models;

namespace BookingAgent.App.Services;

public interface ICruisePricingService
{
    Task<CruisePricingResult> GetLatestPricingAsync();
    Task<CruisePricingResult> GetPriceAsync(CruisePriceCriteria criteria);
}

public sealed class SampleCruisePricingService : ICruisePricingService
{
    public Task<CruisePricingResult> GetLatestPricingAsync()
    {
        return Task.FromResult(BuildSample());
    }

    public Task<CruisePricingResult> GetPriceAsync(CruisePriceCriteria criteria)
    {
        return Task.FromResult(BuildSample());
    }

    internal static CruisePricingResult BuildSample()
    {
        var bookingStart = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));
        var paymentDate1 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
        var paymentDate2 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(35));

        return new CruisePricingResult
        {
            ReservationId = "RC-123456",
            SailingInfo = new CruiseSailingInfo
            {
                SelectedSailing = new SelectedSailing
                {
                    Start = bookingStart,
                    ShipCode = "FR",
                    VendorCode = "RCC",
                    Duration = "P7N"
                },
                InclusivePackageOption = new InclusivePackageOption
                {
                    CruisePackageCode = "FR07W032"
                },
                SelectedCategory = new BookingSelectedCategory
                {
                    FareCode = "BESTRATE",
                    PromotionDescription = "Best available rate with Resident Discount",
                    PricedCategoryCode = "L",
                    BerthedCategoryCode = "L",
                    WaitlistIndicator = false,
                    SelectedCabin = new SelectedCabin
                    {
                        CabinNumber = "1289",
                        Status = "48",
                        CategoryLocation = "Outside",
                        MaxOccupancy = 4
                    },
                    Promotions = new List<PromotionDetail>
                    {
                        new()
                        {
                            PromotionCode = "BESTRATE",
                            Description = "Best Rate Guaranteed"
                        },
                        new()
                        {
                            PromotionCode = "RESIDENT",
                            Description = "Resident Discount",
                            NonRefundableType = "1"
                        }
                    }
                }
            },
            BookingPayment = new BookingPayment
            {
                BookingPrices = new List<BookingPrice>
                {
                    new() { Amount = 1200.00m, PriceTypeCode = "3" },
                    new() { Amount = 250.00m, PriceTypeCode = "6" },
                    new() { Amount = 1498.70m, PriceTypeCode = "100", RestrictedIndicator = true },
                    new() { Amount = 1498.70m, PriceTypeCode = "8" },
                    new() { Amount = 1498.70m, PriceTypeCode = "42" }
                },
                PaymentSchedule = new PaymentSchedule
                {
                    Payments = new List<Payment>
                    {
                        new() { Amount = 500.00m, DueDate = paymentDate1, PaymentNumber = 1 },
                        new() { Amount = 2483.10m, DueDate = paymentDate2, PaymentNumber = 2 }
                    }
                },
                GuestPrices = new List<GuestPrice>
                {
                    new()
                    {
                        GuestRefNumber = 1,
                        GivenName = "Thu",
                        Surname = "Nguyen",
                        Nationality = "VN",
                        PriceInfos = new List<GuestPriceInfo>
                        {
                            new()
                            {
                                Amount = 1498.70m,
                                PriceTypeCode = "100",
                                PriceComponentCode = "CRUISE",
                                PricingLevel = "10",
                                PricingType = "1",
                                AutoAddedIndicator = false
                            },
                            new()
                            {
                                Amount = 50.00m,
                                PriceTypeCode = "127",
                                PriceComponentCode = "FUEL",
                                AutoAddedIndicator = true,
                                MandatoryIndicator = true
                            }
                        }
                    },
                    new()
                    {
                        GuestRefNumber = 2,
                        GivenName = "An",
                        Surname = "Le",
                        Nationality = "VN",
                        PriceInfos = new List<GuestPriceInfo>
                        {
                            new()
                            {
                                Amount = 1498.70m,
                                PriceTypeCode = "100",
                                PriceComponentCode = "CRUISE",
                                PricingLevel = "10",
                                PricingType = "1"
                            },
                            new()
                            {
                                Amount = 22.50m,
                                PriceTypeCode = "108",
                                PriceComponentCode = "OBC",
                                AutoAddedIndicator = true,
                                SelectedOptionsIndicator = true
                            }
                        }
                    }
                }
            }
        };
    }
}
