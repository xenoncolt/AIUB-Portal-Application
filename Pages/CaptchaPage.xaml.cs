using AIUB.Portal.Services;
using System.Threading.Tasks;

namespace AIUB.Portal.Pages;

public partial class CaptchaPage : ContentPage
{
	private readonly IAIUBPortalService _portalService;
	private readonly string _username;
	private readonly string _password;
	private readonly string _captchaImageUrl;
	private readonly string _captchaId;

    // Default colors
    private readonly Color _normalBackgroundColor = Colors.Transparent;
    private readonly Color _errorBackgroundColor = Color.FromArgb("#FFF0F0");
    private readonly Color _normalPlaceholderColor = Colors.DimGrey;
    private readonly Color _errorPlaceholderColor = Colors.Red;

    public CaptchaPage(IAIUBPortalService portalService, string username, string password, string captchaImageUrl, string captchaId)
	{
		InitializeComponent();
		_portalService = portalService;
		_username = username;
		_password = password;
		_captchaImageUrl = captchaImageUrl;
		_captchaId = captchaId;

		AnimateCaptchaCard();
		LoadCaptchaImage();
	}

    private void AnimateCaptchaCard(uint duration = 800)
    {
        this.Opacity = 0;
        this.Scale = 0.8;

        this.FadeTo(1, duration);
        this.ScaleTo(1, duration, Easing.SpringOut);
    }

    private async void LoadCaptchaImage()
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var imageStream = await httpClient.GetStreamAsync(_captchaImageUrl);
                CaptchaImage.Source = ImageSource.FromStream(() => imageStream);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load captcha image: {ex.Message}", "OK");
        }
    }

    private void Entry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry)
        {
            entry.BackgroundColor = _normalBackgroundColor;
            entry.PlaceholderColor = _normalPlaceholderColor;
            entry.Placeholder = "Enter captcha code";
        }
    }

    private async void Submit_Button_Clicked(object sender, EventArgs e)
    {
        try
        {
            var captchaCode = CaptchaEntry.Text?.Trim();

            // Validate captcha input
            if (string.IsNullOrEmpty(captchaCode))
            {
                CaptchaEntry.Text = string.Empty;
                CaptchaEntry.BackgroundColor = _errorBackgroundColor;
                CaptchaEntry.PlaceholderColor = _errorPlaceholderColor;
                CaptchaEntry.Placeholder = "Captcha code is required";

                await CaptchaEntry.TranslateTo(-5, 0, 50);
                await CaptchaEntry.TranslateTo(5, 0, 50);
                await CaptchaEntry.TranslateTo(-5, 0, 50);
                await CaptchaEntry.TranslateTo(0, 0, 50);

                return;
            }

            // Show loading indicator
            CaptchaIndicator.IsVisible = true;
            CaptchaIndicator.IsRunning = true;
            SubmitButton.IsVisible = false;
            CancelButton.IsVisible = false;

            // Submit captcha and login
            var (success, msg, result) = await _portalService.SubmitCaptchaAsync(_username, _password, captchaCode, _captchaId);

            if (success)
            {
                // Store user credentials in secure storage
                await SecureStorage.SetAsync("username", _username);
                await SecureStorage.SetAsync("password", _password);

                // Navigate to main page
                if (Application.Current?.Windows?.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                }
            }
            else
            {
                // Show error message
                await DisplayAlert("Error", msg, "OK");

                // If it was a captcha error, reload the page
                if (msg.Contains("captcha"))
                {
                    var loginResult = await _portalService.LoginAsync(_username, _password);
                    if (!loginResult.success && !string.IsNullOrEmpty(loginResult.captchaImageUrl))
                    {
                        await Navigation.PushModalAsync(new CaptchaPage(_portalService, _username, _password,
                            loginResult.captchaImageUrl, loginResult.captchaId));
                    }
                }

                CaptchaIndicator.IsVisible = false;
                CaptchaIndicator.IsRunning = false;
                SubmitButton.IsVisible = true;
                CancelButton.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            CaptchaIndicator.IsVisible = false;
            CaptchaIndicator.IsRunning = false;
            SubmitButton.IsVisible = true;
            CancelButton.IsVisible = true;

            await DisplayAlert("Error", $"Captcha submission failed: {ex.Message}", "OK");
        }
    }

    private async void Cancel_Button_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}