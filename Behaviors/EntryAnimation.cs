namespace AIUB.Portal.Behaviors;

public class EntryAnimation : Behavior<Entry>
{
	public static readonly BindableProperty FromProperty = BindableProperty.Create(nameof(From), typeof(double), typeof(EntryAnimation), 0.0);
    public static readonly BindableProperty ToProperty = BindableProperty.Create(nameof(To), typeof(double), typeof(EntryAnimation), 1.0);
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(uint), typeof(EntryAnimation), (uint)250);

    public double From
	{
		get => (double)GetValue(FromProperty);
		set => SetValue(FromProperty, value);
	}

	public double To
	{
		get => (double)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
	}

    public uint Duration
    {
        get => (uint)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

	protected override void OnAttachedTo(Entry entry)
	{
		base.OnAttachedTo(entry);
		entry.Opacity = From;
		entry.TranslationY = 20;

		entry.FadeTo(To, Duration);
		entry.TranslateTo(0, 0, Duration, Easing.SinOut);
	}

	protected override void OnDetachingFrom(Entry entry)
	{
		base.OnDetachingFrom(entry);
	}
}