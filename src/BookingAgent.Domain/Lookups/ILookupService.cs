using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BookingAgent.Domain.Lookups;

public interface ILookupService
{
    Task<IReadOnlyList<ShipLookup>> GetShipsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeckLookup>> GetDecksAsync(string shipCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegionLookup>> GetRegionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubRegionLookup>> GetSubRegionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortLookup>> GetPortsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CabinCategoryLookup>> GetCabinCategoriesAsync(string shipCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CabinConfigLookup>> GetCabinConfigsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BedTypeLookup>> GetBedTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LanguageLookup>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GatewayLookup>> GetGatewaysAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TitleLookup>> GetTitlesAsync(CancellationToken cancellationToken = default);
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);
}
