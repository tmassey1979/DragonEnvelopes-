namespace DragonEnvelopes.Desktop.Auth;

public enum LoginWelcomeAction
{
    GetStarted = 0,
    SignIn = 1
}

public enum LoginEntryRoute
{
    CreateFamilyAccount = 0,
    SignInForm = 1
}

public sealed class LoginEntryState
{
    public bool IsSignInViewVisible { get; private set; }

    public LoginEntryRoute HandleWelcomeAction(LoginWelcomeAction action)
    {
        return action switch
        {
            LoginWelcomeAction.GetStarted => LoginEntryRoute.CreateFamilyAccount,
            LoginWelcomeAction.SignIn => RouteToSignInForm(),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported welcome action.")
        };
    }

    public void ShowWelcomeView()
    {
        IsSignInViewVisible = false;
    }

    public void ShowSignInView()
    {
        IsSignInViewVisible = true;
    }

    private LoginEntryRoute RouteToSignInForm()
    {
        IsSignInViewVisible = true;
        return LoginEntryRoute.SignInForm;
    }
}
