using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace Farm.Web.Core.Auth;

/// <summary>Attaches the stored JWT as a Bearer token to every outgoing request on a typed HttpClient.</summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;

    public AuthTokenHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _jsRuntime.InvokeAsync<string?>("farmAuth.get");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
