using AIUB.Portal.Services;
using System.Text.RegularExpressions;

namespace AIUB.Portal.Pages;

public partial class LoginPage : ContentPage
{
    private readonly Regex _usernamePattern = new Regex(@"^\d{2}-\d{5}-\d{1}$|^\d{4}-\d{3}-\d{1}$");
    private readonly IAIUBPortalService _portalService;

    // Default colors
    private readonly Color _normalBackgroundColor = Colors.Transparent;
    private readonly Color _errorBackgroundColor = Color.FromArgb("#FFF0F0");
    private readonly Color _normalPlaceholderColor = Colors.DimGrey;
    private readonly Color _errorPlaceholderColor = Colors.Red;
    public LoginPage(IAIUBPortalService portalService)
    {
        InitializeComponent();
        _portalService = portalService;
        AnimateLoginCard();
    }

    private void Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            entry.BackgroundColor = _normalBackgroundColor;

            if (entry == UsernameEntry)
            {
                entry.PlaceholderColor = _normalPlaceholderColor;
                entry.Placeholder = "Username";
            }
            else if (entry == PasswordEntry)
            {
                entry.PlaceholderColor = _normalPlaceholderColor;
                entry.Placeholder = "Password";
            }
        }
    }

    private async void Login_Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            bool isValid = true;
            var username = UsernameEntry.Text?.Trim();
            var password = PasswordEntry.Text;

            // Validate username
            if (string.IsNullOrEmpty(username))
            {
                UsernameEntry.Text = string.Empty;
                UsernameEntry.BackgroundColor = _errorBackgroundColor;
                UsernameEntry.PlaceholderColor = _errorPlaceholderColor;
                UsernameEntry.Placeholder = "Username is required";
                //UsernameEntry.Unfocus();
                isValid = false;
            } else if (!_usernamePattern.IsMatch(username))
            {
                UsernameEntry.Text = string.Empty;
                UsernameEntry.BackgroundColor = _errorBackgroundColor;
                UsernameEntry.PlaceholderColor = _errorPlaceholderColor;
                UsernameEntry.Placeholder = "Enter ID in XX-XXXXX-X format";
                //UsernameEntry.Unfocus();
                isValid = false;
            }

            // Validate password
            if (string.IsNullOrEmpty(password))
            {
                PasswordEntry.Text = string.Empty;
                PasswordEntry.BackgroundColor = _errorBackgroundColor;
                PasswordEntry.PlaceholderColor = _errorPlaceholderColor;
                PasswordEntry.Placeholder = "Password is required";
                //UsernameEntry.Unfocus();
                isValid = false;
            }

            //this.Focus();

            // If Validation failed then return
            if (!isValid)
            {
                // Subtle shake animation for invalid fields
                var invalidControls = new List<View>();

                if (UsernameEntry.BackgroundColor == _errorBackgroundColor)
                {
                    invalidControls.Add(UsernameEntry);
                }

                if (PasswordEntry.BackgroundColor == _errorBackgroundColor)
                {
                    invalidControls.Add(PasswordEntry);
                }

                foreach (var control in invalidControls)
                {
                    await control.TranslateTo(-5, 0, 50);
                    await control.TranslateTo(5, 0, 50);
                    await control.TranslateTo(-5, 0, 50);
                    await control.TranslateTo(0, 0, 50);
                }

                return;
            }

            // Show Loading Indicator
            LoginIndicator.IsVisible = true;
            LoginIndicator.IsRunning = true;
            LoginButton.IsVisible = false;

            // Perform login using the portal
            var loginResult = await _portalService.LoginAsync(username, password);

            if (loginResult.success)
            {
                // Store user credentials in secure storage
                await SecureStorage.SetAsync("username", username);
                await SecureStorage.SetAsync("password", password);

                // Store portal data in app preferences or secure storage as needed
                // You can serialize the result dictionary to JSON and store it
                // Example: await SecureStorage.SetAsync("portalData", JsonSerializer.Serialize(result));

                if (Application.Current?.Windows?.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                }
            } else if (!string.IsNullOrEmpty(loginResult.captchaImageUrl))
            {
                // Captcha handling
                LoginIndicator.IsVisible = false;
                LoginIndicator.IsRunning = false;
                LoginButton.IsVisible = true;

                await Navigation.PushModalAsync(new CaptchaPage(_portalService, username, password, loginResult.captchaImageUrl, loginResult.captchaId));
            } else
            {
                // Show error message
                await DisplayAlert("Error", loginResult.msg, "OK");

                LoginIndicator.IsVisible = false;
                LoginIndicator.IsRunning = false;
                LoginButton.IsVisible = true;
            }

            // await Task.Delay(2000);

            //// Fix for CS0618 and CS8602
            //if (Application.Current?.Windows?.Count > 0)
            //{
            //    Application.Current.Windows[0].Page = new AppShell();
            //}

            //// Fetch all the portal student locally save and use it later

            //// Login successfull then navigate to the next page
            //// await Shell.Current.GoToAsync();

            //LoginIndicator.IsVisible = true;
            //LoginIndicator.IsRunning = true;
        } catch (Exception ex)
        {
            LoginIndicator.IsVisible = false;
            LoginIndicator.IsRunning = false;
            LoginButton.IsVisible = true;

            await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }
    }

    private void AnimateLoginCard(uint duration = 800)
    {
        this.Opacity = 0;
        this.Scale = 0.8;

        this.FadeTo(1, duration);
        this.ScaleTo(1, duration, Easing.SpringOut);
    }
    //private async Task PerformLogin(string username, string password)
    //{
    //    // Fetch all the portal student locally save and use it later

    //    //await Shell.Current.GoToAsync();
    //    await DisplayAlert("loginDone", "Already Login Done", "OK");
    //}
}
