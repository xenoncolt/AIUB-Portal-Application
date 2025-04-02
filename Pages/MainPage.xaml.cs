using AIUB.Portal.Services;
using System.Threading.Tasks;

namespace AIUB.Portal.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Logout_Button_Clicked(object sender, EventArgs e)
        {
            bool confirm_logout = await DisplayAlert("Confirm Logout", "Are you sure you want to logout?", "Yes", "No");

            if (confirm_logout)
            {
                SecureStorage.Remove("username");
                SecureStorage.Remove("password");

                if (Application.Current?.Windows.Count > 0)
                {
                    var portalService = IPlatformApplication.Current?.Services.GetService<IAIUBPortalService>();
                    Application.Current.Windows[0].Page = new LoginPage(portalService);
                }
            }
        }
    }
}
