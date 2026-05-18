namespace FantasyFootball.UI.Helpers;

/// <summary>
/// Web-host flag URL helper. Keyed off ISO 3166-1 alpha-2 country codes
/// (<c>Country.Code2</c>), serves SVGs from flagcdn.com so the same asset stays
/// crisp from list rows to detail headers and avoids shipping ~250 PNGs in the
/// WASM payload. URL pattern hinted at in <c>IconStrings</c>.
/// </summary>
public static class FlagHelper
{
  public static string Url(string code2) => $"https://flagcdn.com/{code2.ToLower()}.svg";
}
