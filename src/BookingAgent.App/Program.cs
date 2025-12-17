using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BookingAgent.Domain.Lookups;
using BookingAgent.App.Services;
using BookingAgent.Domain.Config;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.Configure<RoyalCaribbeanApiOptions>(builder.Configuration.GetSection("RoyalCaribbeanApi"));
builder.Services.AddHttpClient<ICruisePricingService, RoyalCaribbeanSoapPricingClient>();
builder.Services.AddHttpClient<ISailingListService, SailingListService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILookupService, LookupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
