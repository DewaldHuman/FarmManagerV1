using System.Globalization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Farm.Web.Core.Auth;
using Farm.Web.Core.Localization;
using Farm.Web.Host;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalization();

// Auth
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, FarmAuthStateProvider>();
builder.Services.AddTransient<AuthTokenHandler>();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthTokenHandler>(); // needed for GetCurrentUserAsync (/auth/me), called from LoginAsync after the token is stored

// Language — typed client (not AddScoped) since SetLanguageAsync now also
// PATCHes the backend to persist the preference per-user (localStorage stays
// the fast local cache driving the synchronous pre-render culture bootstrap below).
builder.Services.AddHttpClient<ILanguageService, LanguageService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthTokenHandler>();

// Farm registry — attaches the stored JWT to every request via AuthTokenHandler
// (AuthService never needed this: /auth/login is anonymous and nothing else
// called an authenticated endpoint until this feature).
builder.Services.AddHttpClient<Farm.Web.Core.Registry.IFarmService, Farm.Web.Core.Registry.FarmService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthTokenHandler>();

// Irrigation calculation-run logging — lives in Farm.Web.Core (not the
// lazy-loaded Farm.Web.Irrigation) because Program.cs runs eagerly at startup;
// referencing a type from a lazy-loaded assembly here would force it to load
// before its .wasm is fetched, breaking lazy loading entirely (confirmed by
// hitting exactly this "Could not load file or assembly" error in testing).
builder.Services.AddHttpClient<Farm.Web.Core.Irrigation.IIrrigationService, Farm.Web.Core.Irrigation.IrrigationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<AuthTokenHandler>();

// Zone Designer's in-memory design store — also lives in Farm.Web.Core (see
// note above re: lazy assemblies) because ZoneOverview.razor reads design
// status. Singleton so drawn layouts survive in-app navigation; swap for an
// HTTP-backed implementation when backend persistence lands.
builder.Services.AddSingleton<Farm.Web.Core.Irrigation.IZoneDesignStore, Farm.Web.Core.Irrigation.InMemoryZoneDesignStore>();

var host = builder.Build();

// Must run after Build() (so IJSRuntime is resolvable) but before RunAsync()
// (so the culture is set before any component renders). Auth doesn't need this
// same pre-render timing: CascadingAuthenticationState resolves its
// Task<AuthenticationState> asynchronously after the app starts rendering, and
// FarmAuthStateProvider does its own JS interop call lazily the first time it's asked.
var jsRuntime = host.Services.GetRequiredService<IJSRuntime>();
var storedCulture = await jsRuntime.InvokeAsync<string>("farmCulture.get");
var culture = new CultureInfo(storedCulture);

// Note: CurrentCulture and CurrentUICulture are kept in sync here (Blazor WASM's
// culture-change guard rejects a startup culture that diverges between the two).
// Numeric/date formatting elsewhere stays safe regardless: RunCalculator.razor
// already formats/parses every number with an explicit CultureInfo.InvariantCulture
// argument, so it is unaffected by CurrentCulture being "af".
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await host.RunAsync();
