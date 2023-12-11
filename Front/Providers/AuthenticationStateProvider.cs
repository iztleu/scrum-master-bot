using System.Security.Claims;
using Front.Models;
using Front.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Front.Providers;

public class ScrumMasterBotAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ScrumMusterBotUserService _userService;
    public User CurrentUser { get; private set; } = new();
    
    public ScrumMasterBotAuthenticationStateProvider(ScrumMusterBotUserService userService)
    {
        _userService = userService;
        AuthenticationStateChanged += OnAuthenticationStateChangedAsync;
    }
    
    public async Task SendVerifyCodeAsync(string username)
    {
        await _userService.SendVerifyCode(username);
    }
    
    public async Task LoginAsync(string username, string password)
    {
        var principal = new ClaimsPrincipal();
        var user = await _userService.SendAuthenticateRequestAsync(username, password);

        if (user is not null)
        {
            principal = user.ToClaimsPrincipal();
        }

        CurrentUser = user;
        
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var principal = new ClaimsPrincipal();
        var user = _userService.FetchUserFromBrowser();

        if (user is not null)
        {
            // var authenticatedUser = await _userService.SendAuthenticateRequestAsync(user.Username, user.Password);
            //
            // if (authenticatedUser is not null)
            // {
            //     principal = authenticatedUser.ToClaimsPrincipal();
            // }
            
            CurrentUser = user;
        }

        return new(principal);
    }
    
    private async void OnAuthenticationStateChangedAsync(Task<AuthenticationState> task)
    {
        var authenticationState = await task;

        if (authenticationState is not null)
        {
            CurrentUser = User.FromClaimsPrincipal(authenticationState.User);
        }
    }
    
    public void Logout()
    {
        _userService.ClearBrowserUserData();
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new())));
    }
    
}