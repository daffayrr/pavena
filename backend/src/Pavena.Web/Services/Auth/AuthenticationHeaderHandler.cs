using System.Net.Http.Headers;

namespace Pavena.Web.Services.Auth;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly IScholarSessionService _sessionService;

    public AuthenticationHeaderHandler(IScholarSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _sessionService.AccessToken;
        if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization == null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
