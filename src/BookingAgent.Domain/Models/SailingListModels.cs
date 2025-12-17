using System;

namespace BookingAgent.Domain.Models;

public record SailingOptionResult
{
    public string? CruisePackageCode { get; init; }
    public string? ShipCode { get; init; }
    public string? VendorCode { get; init; }
    public string? RegionCode { get; init; }
    public string? SubRegionCode { get; init; }
    public string? DeparturePort { get; init; }
    public string? ArrivalPort { get; init; }
    public DateOnly? StartDate { get; init; }
    public string? Duration { get; init; }
}
