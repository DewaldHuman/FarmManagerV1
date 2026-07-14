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
builder.Services.AddScoped<ILanguageService, LanguageService>();

// Auth
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, FarmAuthStateProvider>();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

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
