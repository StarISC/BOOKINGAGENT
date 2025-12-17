using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using BookingAgent.Domain.Lookups;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookingAgent.App.Services;

public sealed class LookupService : ILookupService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LookupService> _logger;
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    public LookupService(IMemoryCache cache, IConfiguration configuration, ILogger<LookupService> logger)
    {
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<IReadOnlyList<ShipLookup>> GetShipsAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_ships", LoadShipsAsync, cancellationToken);

    public Task<IReadOnlyList<DeckLookup>> GetDecksAsync(string shipCode, CancellationToken cancellationToken = default) =>
        GetOrLoadAsync($"lookup_decks_{shipCode}", ct => LoadDecksAsync(shipCode, ct), cancellationToken);

    public Task<IReadOnlyList<RegionLookup>> GetRegionsAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_regions", LoadRegionsAsync, cancellationToken);

    public Task<IReadOnlyList<SubRegionLookup>> GetSubRegionsAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_subregions", LoadSubRegionsAsync, cancellationToken);

    public Task<IReadOnlyList<PortLookup>> GetPortsAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_ports", LoadPortsAsync, cancellationToken);

    public Task<IReadOnlyList<CabinCategoryLookup>> GetCabinCategoriesAsync(string shipCode, CancellationToken cancellationToken = default) =>
        GetOrLoadAsync($"lookup_cabincategories_{shipCode}", ct => LoadCabinCategoriesAsync(shipCode, ct), cancellationToken);

    public Task<IReadOnlyList<CabinConfigLookup>> GetCabinConfigsAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_cabinconfigs", LoadCabinConfigsAsync, cancellationToken);

    public Task<IReadOnlyList<BedTypeLookup>> GetBedTypesAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_bedtypes", LoadBedTypesAsync, cancellationToken);

    public Task<IReadOnlyList<LanguageLookup>> GetLanguagesAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_languages", LoadLanguagesAsync, cancellationToken);

    public Task<IReadOnlyList<GatewayLookup>> GetGatewaysAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_gateways", LoadGatewaysAsync, cancellationToken);

    public Task<IReadOnlyList<TitleLookup>> GetTitlesAsync(CancellationToken cancellationToken = default) =>
        GetOrLoadAsync("lookup_titles", LoadTitlesAsync, cancellationToken);

    public Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        _cache.Remove("lookup_ships");
        _cache.Remove("lookup_regions");
        _cache.Remove("lookup_subregions");
        _cache.Remove("lookup_ports");
        _cache.Remove("lookup_cabinconfigs");
        _cache.Remove("lookup_bedtypes");
        _cache.Remove("lookup_languages");
        _cache.Remove("lookup_gateways");
        _cache.Remove("lookup_titles");
        // Decks and categories are per-ship; they will repopulate on demand.
        return Task.CompletedTask;
    }

    private async Task<IReadOnlyList<T>> GetOrLoadAsync<T>(string cacheKey, Func<CancellationToken, Task<IReadOnlyList<T>>> loader, CancellationToken ct)
    {
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<T>? cached) && cached is not null)
        {
            return cached;
        }

        var data = await loader(ct).ConfigureAwait(false);
        _cache.Set(cacheKey, data, CacheOptions);
        return data;
    }

    private async Task<IReadOnlyList<ShipLookup>> LoadShipsAsync(CancellationToken ct)
    {
        const string sql = @"SELECT BrandCode, ShipCode, ShipName FROM dbo.Ships ORDER BY ShipName";
        var list = new List<ShipLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new ShipLookup
            {
                BrandCode = reader.GetString(0),
                ShipCode = reader.GetString(1),
                ShipName = reader.GetString(2)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<DeckLookup>> LoadDecksAsync(string shipCode, CancellationToken ct)
    {
        const string sql = @"SELECT BrandCode, ShipCode, DeckCode, DeckName, DeckNumber FROM dbo.Decks WHERE ShipCode = @ShipCode ORDER BY DeckNumber";
        var list = new List<DeckLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@ShipCode", SqlDbType.NVarChar, 10) { Value = shipCode });
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DeckLookup
            {
                BrandCode = reader.GetString(0),
                ShipCode = reader.GetString(1),
                DeckCode = reader.GetString(2),
                DeckName = reader.GetString(3),
                DeckNumber = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<RegionLookup>> LoadRegionsAsync(CancellationToken ct)
    {
        const string sql = @"SELECT RegionCode, RegionName FROM dbo.Regions ORDER BY RegionName";
        var list = new List<RegionLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new RegionLookup
            {
                RegionCode = reader.GetString(0),
                RegionName = reader.GetString(1)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<SubRegionLookup>> LoadSubRegionsAsync(CancellationToken ct)
    {
        const string sql = @"SELECT SubRegionCode, Description FROM dbo.SubRegions ORDER BY Description";
        var list = new List<SubRegionLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new SubRegionLookup
            {
                SubRegionCode = reader.GetString(0),
                Description = reader.GetString(1)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<PortLookup>> LoadPortsAsync(CancellationToken ct)
    {
        const string sql = @"SELECT PortCode, PortName, CountryCode FROM dbo.Ports ORDER BY PortName";
        var list = new List<PortLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PortLookup
            {
                PortCode = reader.GetString(0),
                PortName = reader.GetString(1),
                CountryCode = reader.GetString(2)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<CabinCategoryLookup>> LoadCabinCategoriesAsync(string shipCode, CancellationToken ct)
    {
        const string sql = @"SELECT BrandCode, ShipCode, CategoryCode, Description, InsideOutside FROM dbo.CabinCategories WHERE ShipCode = @ShipCode ORDER BY CategoryCode";
        var list = new List<CabinCategoryLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@ShipCode", SqlDbType.NVarChar, 10) { Value = shipCode });
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new CabinCategoryLookup
            {
                BrandCode = reader.GetString(0),
                ShipCode = reader.GetString(1),
                CategoryCode = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                InsideOutside = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<CabinConfigLookup>> LoadCabinConfigsAsync(CancellationToken ct)
    {
        const string sql = @"SELECT CabinConfigCode, Description FROM dbo.CabinConfigs ORDER BY CabinConfigCode";
        var list = new List<CabinConfigLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new CabinConfigLookup
            {
                CabinConfigCode = reader.GetString(0),
                Description = reader.GetString(1)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<BedTypeLookup>> LoadBedTypesAsync(CancellationToken ct)
    {
        const string sql = @"SELECT BedTypeCode, Description FROM dbo.BedTypes ORDER BY BedTypeCode";
        var list = new List<BedTypeLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new BedTypeLookup
            {
                BedTypeCode = reader.GetString(0),
                Description = reader.GetString(1)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<LanguageLookup>> LoadLanguagesAsync(CancellationToken ct)
    {
        const string sql = @"SELECT LanguageCode, Description FROM dbo.Languages ORDER BY LanguageCode";
        var list = new List<LanguageLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new LanguageLookup
            {
                LanguageCode = reader.GetString(0),
                Description = reader.GetString(1)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<GatewayLookup>> LoadGatewaysAsync(CancellationToken ct)
    {
        const string sql = @"SELECT AirportCode, AirportName, AirportCity, AirportState, AirportCountry FROM dbo.Gateways ORDER BY AirportCode";
        var list = new List<GatewayLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new GatewayLookup
            {
                AirportCode = reader.GetString(0),
                AirportName = reader.GetString(1),
                AirportCity = reader.IsDBNull(2) ? null : reader.GetString(2),
                AirportState = reader.IsDBNull(3) ? null : reader.GetString(3),
                AirportCountry = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }
        return list;
    }

    private async Task<IReadOnlyList<TitleLookup>> LoadTitlesAsync(CancellationToken ct)
    {
        const string sql = @"SELECT TitleCode, Description, Dpcdty FROM dbo.Titles ORDER BY TitleCode";
        var list = new List<TitleLookup>();
        await using var conn = await OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new TitleLookup
            {
                TitleCode = reader.GetString(0),
                Description = reader.GetString(1),
                Dpcdty = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return list;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var cs = _configuration.GetConnectionString("BookingAgent");
        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Connection string 'BookingAgent' is not configured.");
        }

        var conn = new SqlConnection(cs);
        try
        {
            await conn.OpenAsync(ct).ConfigureAwait(false);
            return conn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open SQL connection for lookups.");
            throw;
        }
    }
}
