namespace BookingAgent.Domain.Config;

public class RoyalCaribbeanApiOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VendorCode { get; set; } = "RCC";
    public string CompanyShortName { get; set; } = string.Empty;
    public string RequestorId { get; set; } = "275611";
    public string TerminalId { get; set; } = "JOHN12";
    public string OperationPath { get; set; } = "BookingPrice";
    public string SoapAction { get; set; } = string.Empty;
    public bool UseStub { get; set; } = true;
}
