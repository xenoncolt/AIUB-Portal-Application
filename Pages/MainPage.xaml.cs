using AIUB.Portal.Services;
using System.Diagnostics;
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

        // Add this method to MainPage.xaml.cs
        private async void RefreshView_Refreshing(object sender, EventArgs e)
        {
            try
            {
                if (App.IsSessionRefreshNeeded)
                {
                    bool refreshed = await App.RefreshSessionAsync();
                    if (!refreshed)
                    {
                        await DisplayAlert("Refresh Failed", "Could not connect to AIUB Portal. Please check your connecting or log in again.", "OK");

                        if (sender is RefreshView refreshView)
                        {
                            refreshView.IsRefreshing = false;
                        }
                        return;
                    }
                }

                // Now Update UI with fresh data
                await LoadData();
                
                // End refresh ---- using the sender parameter which is the RefreshView instance
                if (sender is RefreshView)
                {
                    RefreshView.IsRefreshing = false;
                }
            }
            catch (Exception ex)
            {
                // Handle exception and ensure refresh indicator is stopped
                if (sender is RefreshView)
                {
                    RefreshView.IsRefreshing = false;
                }
                await DisplayAlert("Error", $"Failed to refresh data: {ex.Message}", "OK");
            }
        }


        private async Task LoadData()
        {
            // Get the portal service
            var portalService = IPlatformApplication.Current?.Services.GetService<IAIUBPortalService>();
            if (portalService == null)
                return;

            // Have to implement data loading logic here
            // This will depend on how app currently displays data

            // Example:
            // var studentData = await _studentService.GetLatestStudentDataAsync();
            // UpdateUI(studentData);
        }

        // Add this to OnAppearing() if you have it
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Optional: You can auto-refresh when the page appears if needed
            if (App.IsSessionRefreshNeeded)
            {
                RefreshView.IsRefreshing = true;
                _ = RefreshAndResetFlag();
            }
        }

        private async Task RefreshAndResetFlag()
        {
            try
            {
                await App.RefreshSessionAsync();
                await LoadData();

                // Make sure that main thread updates when updating UI
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RefreshView.IsRefreshing = false;
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RefreshView.IsRefreshing = false;
                });

                Debug.WriteLine($"Error during refresh: {ex.Message}");
            }
        }
    }
}
