namespace AIUB.Portal.Behaviors;

public class ButtonAnimation : Behavior<Button>
{
	public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(uint), typeof(ButtonAnimation), (uint)250);

	public uint Duration
	{
		get => (uint)GetValue(DurationProperty);
		set => SetValue(DurationProperty, value);
	}

    protected override void OnAttachedTo(Button button)
    {
        base.OnAttachedTo(button);
        button.Opacity = 0;
        button.TranslationY = 20;

        button.FadeTo(1, Duration);
        button.TranslateTo(0, 0, Duration, Easing.CubicOut);

        button.Pressed += OnButtonPressed;
        button.Released += OnButtonReleased;
    }

    protected override void OnDetachingFrom(Button button)
    {
        button.Pressed -= OnButtonPressed;
        button.Released -= OnButtonReleased;
        base.OnDetachingFrom(button);
    }

    private void OnButtonPressed(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.ScaleTo(0.95, 50, Easing.CubicOut);
        }
    }

    private void OnButtonReleased(object sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.ScaleTo(1, 1000, Easing.SpringOut);   
        }
    }
}