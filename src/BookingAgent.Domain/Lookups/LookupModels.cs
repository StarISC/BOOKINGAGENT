using System;

namespace BookingAgent.Domain.Lookups;

public record ShipLookup
{
    public string BrandCode { get; init; } = string.Empty;
    public string ShipCode { get; init; } = string.Empty;
    public string ShipName { get; init; } = string.Empty;
}

public record DeckLookup
{
    public string BrandCode { get; init; } = string.Empty;
    public string ShipCode { get; init; } = string.Empty;
    public string DeckCode { get; init; } = string.Empty;
    public string DeckName { get; init; } = string.Empty;
    public int? DeckNumber { get; init; }
}

public record RegionLookup
{
    public string RegionCode { get; init; } = string.Empty;
    public string RegionName { get; init; } = string.Empty;
}

public record SubRegionLookup
{
    public string SubRegionCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record PortLookup
{
    public string PortCode { get; init; } = string.Empty;
    public string PortName { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
}

public record CabinCategoryLookup
{
    public string BrandCode { get; init; } = string.Empty;
    public string ShipCode { get; init; } = string.Empty;
    public string CategoryCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? InsideOutside { get; init; }
}

public record CabinConfigLookup
{
    public string CabinConfigCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record BedTypeLookup
{
    public string BedTypeCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record LanguageLookup
{
    public string LanguageCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record GatewayLookup
{
    public string AirportCode { get; init; } = string.Empty;
    public string AirportName { get; init; } = string.Empty;
    public string? AirportCity { get; init; }
    public string? AirportState { get; init; }
    public string? AirportCountry { get; init; }
}

public record TitleLookup
{
    public string TitleCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Dpcdty { get; init; }
}
