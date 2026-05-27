namespace FantasyFootball.UI.Helpers;

/// <summary>
/// flagcdn.com SVG keyed off ISO 3166-1 alpha-2 (<c>Country.Code2</c>). Avoids bundling ~250 PNGs in the WASM payload.
///
/// Defunct teams (FIFA 3-letter code <c>Code3</c>) whose deprecated alpha-2 codes aren't served by flagcdn ship as
/// bundled SVGs under <c>wwwroot/img/flags/{code3-lower}.svg</c>. Call <see cref="UrlFor"/> with both codes to opt in.
/// </summary>
public static class FlagHelper
{
	// Defunct national teams with locally-bundled flag SVGs. West Germany (FRG) and Czechoslovakia (TCH) share the modern Germany / Czechia flag respectively and route through flagcdn via their inherited alpha-2 code.
	static readonly HashSet<string> BundledByCode3 = new(StringComparer.OrdinalIgnoreCase)
	{
		"GDR", "URS", "YUG", "SCG", "ZAI", "CIS",
	};

	public static string Url(string code2) => $"https://flagcdn.com/{code2.ToLowerInvariant()}.svg";

	public static string UrlFor(string? code2, string? code3)
	{
		if (!string.IsNullOrEmpty(code3) && BundledByCode3.Contains(code3))
		{
			return $"_content/FantasyFootball.UI/img/flags/{code3.ToLowerInvariant()}.svg";
		}

		return Url(code2 ?? "");
	}
}
