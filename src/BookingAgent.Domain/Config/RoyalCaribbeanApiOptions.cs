namespace BookingAgent.Domain.Config;

public class RoyalCaribbeanApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VendorCode { get; set; } = "RCC";
    public string CompanyShortName { get; set; } = string.Empty;
}
