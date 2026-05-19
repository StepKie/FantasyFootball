namespace FantasyFootball;

public static class ResourceConstants
{
	public static readonly Color DefaultHighlightColor = (Color)Application.Current!.Resources["Primary"];
	public static readonly Color DefaultPageColor = (Color)Application.Current!.Resources["WhiteAccent"];

	public static readonly ImageSource QuestionMark = new FontImageSource { Glyph = IconFont.Question_mark, FontFamily = "MaterialIcons", Color = Colors.Grey };
}
