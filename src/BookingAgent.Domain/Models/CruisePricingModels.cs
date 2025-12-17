using System;
using System.Collections.Generic;

namespace BookingAgent.Domain.Models;

public record CruisePricingResult
{
    public string? ReservationId { get; init; }
    public CruiseSailingInfo? SailingInfo { get; init; }
    public BookingPayment? BookingPayment { get; init; }
}

public record CruisePriceCriteria
{
    public string? RegionCode { get; init; }
    public string? PortCode { get; init; }
    public string? ShipCode { get; init; }
    public string? CabinCategoryCode { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public int? DurationNights { get; init; }
    public string? CurrencyCode { get; init; } = "USD";
}

public record CruiseSailingInfo
{
    public SelectedSailing? SelectedSailing { get; init; }
    public InclusivePackageOption? InclusivePackageOption { get; init; }
    public BookingSelectedCategory? SelectedCategory { get; init; }
}

public record SelectedSailing
{
    public DateOnly? Start { get; init; }
    public string? ShipCode { get; init; }
    public string? VendorCode { get; init; }
    public string? Duration { get; init; }
}

public record InclusivePackageOption
{
    public string? CruisePackageCode { get; init; }
}

public record BookingSelectedCategory
{
    public string? FareCode { get; init; }
    public string? PromotionDescription { get; init; }
    public string? PricedCategoryCode { get; init; }
    public string? BerthedCategoryCode { get; init; }
    public bool? WaitlistIndicator { get; init; }
    public SelectedCabin? SelectedCabin { get; init; }
    public List<PromotionDetail> Promotions { get; init; } = new();
}

public record SelectedCabin
{
    public string? CabinNumber { get; init; }
    public string? Status { get; init; }
    public string? CategoryLocation { get; init; }
    public int? MaxOccupancy { get; init; }
}

public record PromotionDetail
{
    public string? PromotionCode { get; init; }
    public string? Description { get; init; }
    public string? NonRefundableType { get; init; }
}

public record BookingPayment
{
    public List<BookingPrice> BookingPrices { get; init; } = new();
    public PaymentSchedule? PaymentSchedule { get; init; }
    public List<GuestPrice> GuestPrices { get; init; } = new();
}

public record BookingPrice
{
    public decimal? Amount { get; init; }
    public decimal? Percent { get; init; }
    public string? PriceTypeCode { get; init; }
    public string? PriceComponentCode { get; init; }
    public bool? RestrictedIndicator { get; init; }
}

public record PaymentSchedule
{
    public List<Payment> Payments { get; init; } = new();
}

public record Payment
{
    public decimal? Amount { get; init; }
    public DateOnly? DueDate { get; init; }
    public int? PaymentNumber { get; init; }
}

public record GuestPrice
{
    public int? GuestRefNumber { get; init; }
    public string? GivenName { get; init; }
    public string? Surname { get; init; }
    public string? Nationality { get; init; }
    public List<GuestPriceInfo> PriceInfos { get; init; } = new();
}

public record GuestPriceInfo
{
    public decimal? Amount { get; init; }
    public decimal? Percent { get; init; }
    public string? PriceTypeCode { get; init; }
    public string? PricedComponentType { get; init; }
    public string? PriceComponentCode { get; init; }
    public string? CodeDescription { get; init; }
    public string? OptionType { get; init; }
    public string? PricingLevel { get; init; }
    public string? PricingType { get; init; }
    public bool? AutoAddedIndicator { get; init; }
    public bool? SelectedOptionsIndicator { get; init; }
    public bool? MandatoryIndicator { get; init; }
    public bool? ItemizableIndicator { get; init; }
    public string? NonRefundableType { get; init; }
}
