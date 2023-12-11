using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Front.Models;
using Front.Storage;

namespace Front.Services;

public class ScrumMusterBotUserService
{
    private readonly HttpClient _httpClient;
    private readonly MemoryStorage _authenticationDataMemoryStorage;

    public ScrumMusterBotUserService(IHttpClientFactory HttpClientFactory, MemoryStorage authenticationDataMemoryStorage)
    {
        _httpClient = HttpClientFactory.CreateClient("app");
        _authenticationDataMemoryStorage = authenticationDataMemoryStorage;
    }
    
    public async Task SendVerifyCode (string username)
    {
        var response = await _httpClient.GetAsync($"/auth/send-verify-code?username={username}");
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Error sending verify code");
        }
    }
    
    public async Task<User?> SendAuthenticateRequestAsync(string username, string verifycode)
    {
        var response = await _httpClient.PostAsJsonAsync($"/auth/authenticate", new
        {
            username,
            verifycode,
        });

        if (response.IsSuccessStatusCode)
        {

            var authenticateResponse = await response.Content.ReadFromJsonAsync<AuthenticateResponse?>();
            if (authenticateResponse == null) throw new ArgumentNullException(nameof(authenticateResponse));
            var claimPrincipal = CreateClaimsPrincipalFromToken(authenticateResponse.AccessToken);
            var user = User.FromClaimsPrincipal(claimPrincipal);
            PersistUserToBrowser(authenticateResponse.AccessToken);

            return user;
        }

        return null;
    }

    private ClaimsPrincipal CreateClaimsPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var identity = new ClaimsIdentity();

        if (tokenHandler.CanReadToken(token))
        {
            var jwtSecurityToken = tokenHandler.ReadJwtToken(token);
            identity = new(jwtSecurityToken.Claims, "ScrumMaterBotIdentity");
        }

        return new(identity);
    }

    private void PersistUserToBrowser(string token) => _authenticationDataMemoryStorage.Token = token;

    public User? FetchUserFromBrowser()
    {
        var claimsPrincipal = CreateClaimsPrincipalFromToken(_authenticationDataMemoryStorage.Token);
        var user = User.FromClaimsPrincipal(claimsPrincipal);

        return user;
    }
    
    public void ClearBrowserUserData() => _authenticationDataMemoryStorage.Token = "";
}

public record AuthenticateResponse(string AccessToken);