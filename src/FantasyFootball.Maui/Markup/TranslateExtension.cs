namespace FantasyFootball.Markup;

/// <summary>
/// Drop-in replacement for the Xamarin Community Toolkit's <c>xct:Translate</c> markup.
/// Looks up the given key in <see cref="Resources.AppResources"/> at parse time.
/// Usage: <c>Title="{translate:Translate Simulate}"</c>.
/// </summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public sealed class TranslateExtension : IMarkupExtension<string>
{
	public string Key { get; set; } = string.Empty;

	public string ProvideValue(IServiceProvider serviceProvider)
		=> string.IsNullOrEmpty(Key) ? string.Empty : (Res.ResourceManager.GetString(Key) ?? Key);

	object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
