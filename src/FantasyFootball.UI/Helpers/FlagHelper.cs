namespace FantasyFootball.UI.Helpers;

/// <summary>
/// flagcdn.com SVG keyed off ISO 3166-1 alpha-2 (<c>Country.Code2</c>). Avoids bundling ~250 PNGs in the WASM payload.
/// </summary>
public static class FlagHelper
{
  public static string Url(string code2) => $"https://flagcdn.com/{code2.ToLower()}.svg";
}
