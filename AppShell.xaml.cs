using AIUB.Portal.Pages;

namespace AIUB.Portal
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }

        private static void RegisterRoutes()
        {
            Routing.RegisterRoute("login", typeof(LoginPage));
            Routing.RegisterRoute("main", typeof(MainPage));
        }
    }
}
