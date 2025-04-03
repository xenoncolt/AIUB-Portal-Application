using AIUB.Portal.Pages;
using AIUB.Portal.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace AIUB.Portal
{
    public partial class App : Application
    {
        private readonly IAIUBPortalService _portalService;
        public static bool IsSessionRefreshNeeded { get; set; } = false;
        public App(IAIUBPortalService portalService)
        {
            InitializeComponent();
            _portalService = portalService;
        }

        private async Task TryAutoLogin()
        {
            try
            {
                // Try to use existing session
                var sessionValid = await _portalService.TryLoadSavedSession();
                if (sessionValid)
                {
                    Windows[0].Page = new AppShell();
                    return;
                }

                var username = await SecureStorage.GetAsync("username");
                var password = await SecureStorage.GetAsync("password");

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    var loginResult = await _portalService.LoginAsync(username, password);

                    if (loginResult.success)
                    {
                        Windows[0].Page = new AppShell();
                    }
                    // If captcha or other login issues, let the user handle it manually
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Auto-login failed: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window window;
            window = new Window(new AppShell());
            CheckAndSkipToMainPage(window);
            return window;
        }

        private static async void CheckAndSkipToMainPage(Window window)
        {
            try
            {
                var username = await SecureStorage.GetAsync("username");
                var password = await SecureStorage.GetAsync("password");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    // Get the portal service from the service provider
                    var portalService = IPlatformApplication.Current?.Services.GetService<IAIUBPortalService>();
                    window.Page = new LoginPage(portalService);
                }
                else
                {
                    window.Page = new AppShell();
                    IsSessionRefreshNeeded = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during app startup: {ex.Message}");
                var portalService = IPlatformApplication.Current?.Services.GetService<IAIUBPortalService>();
                window.Page = new LoginPage(portalService);
            }
        }

        // Refresh the user session and data by connecting to the AIUB portal
        // This should only be called when explicitly requested by the user
        public static async Task<bool> RefreshSessionAsync()
        {
            try
            {
                var portalService = IPlatformApplication.Current?.Services.GetService<IAIUBPortalService>();
                if (portalService == null) return false;

                // Try to use existing session first to minimize login requests
                var sessionValid = await portalService.TryLoadSavedSession();
                if (sessionValid)
                {
                    IsSessionRefreshNeeded = false;
                    return true;
                }

                // If session is not valid, try to login with saved credentials
                var username = await SecureStorage.GetAsync("username");
                var password = await SecureStorage.GetAsync("password");

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    var loginResult = await portalService.LoginAsync(username, password);

                    if (loginResult.success)
                    {
                        IsSessionRefreshNeeded = false;
                        return true;
                    }

                    // Handle captcha case or login failure
                    if (!string.IsNullOrEmpty(loginResult.captchaImageUrl))
                    {
                        var rootPage = Application.Current?.Windows[0].Page;
                        if (rootPage != null)
                        {
                            await rootPage.Navigation.PushModalAsync(
                                new CaptchaPage(portalService, username, password, loginResult.captchaImageUrl, loginResult.captchaId));
                        }
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Session refresh failedL: {ex.Message}");
                return false;
            }
        }
    }
}