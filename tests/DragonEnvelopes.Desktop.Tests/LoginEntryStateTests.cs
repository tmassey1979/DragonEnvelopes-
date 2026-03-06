using DragonEnvelopes.Desktop.Auth;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class LoginEntryStateTests
{
    [Fact]
    public void HandleWelcomeAction_SignIn_RoutesToSignInForm_AndShowsSignInView()
    {
        var state = new LoginEntryState();

        var route = state.HandleWelcomeAction(LoginWelcomeAction.SignIn);

        Assert.Equal(LoginEntryRoute.SignInForm, route);
        Assert.True(state.IsSignInViewVisible);
    }

    [Fact]
    public void HandleWelcomeAction_GetStarted_RoutesToCreateFamilyAccount()
    {
        var state = new LoginEntryState();

        var route = state.HandleWelcomeAction(LoginWelcomeAction.GetStarted);

        Assert.Equal(LoginEntryRoute.CreateFamilyAccount, route);
        Assert.False(state.IsSignInViewVisible);
    }

    [Fact]
    public void ShowWelcomeView_ResetsSignInVisibility()
    {
        var state = new LoginEntryState();
        state.ShowSignInView();

        state.ShowWelcomeView();

        Assert.False(state.IsSignInViewVisible);
    }
}
