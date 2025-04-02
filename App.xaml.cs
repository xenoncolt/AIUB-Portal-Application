using AIUB.Portal.Pages;
using AIUB.Portal.Services;

namespace AIUB.Portal
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
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
                }
            }
            catch (Exception)
            {
                var portalService = IPlatformApplication.Current?.Services.GetService<IAIUBPortalService>();
                window.Page = new LoginPage(portalService);
            }
        }
    }
}