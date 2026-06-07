namespace FantasyFootball.UI.Helpers;

/// <summary>
/// Inserts a soft hyphen (U+00AD) at one natural break point of known long
/// display-name words, so a narrow scoreboard cell wraps them with a visible
/// hyphen ("Nether-lands") instead of a bare mid-word cut. Curated because
/// browsers deliberately refuse to auto-hyphenate capitalized English words
/// (proper-noun protection) — which covers every team name. Render-time only;
/// stored data and lookups stay clean of the marker.
/// </summary>
public static class DisplayHyphenation
{
	const string Shy = "­";

	// Every word ≥ 9 chars appearing in countries.json (en) or clubs.json names.
	static readonly Dictionary<string, string> Words = new(StringComparer.Ordinal)
	{
		["Afghanistan"] = $"Afghan{Shy}istan",
		["Barcelona"] = $"Barce{Shy}lona",
		["Brentford"] = $"Brent{Shy}ford",
		["Cremonese"] = $"Cremo{Shy}nese",
		["Deportivo"] = $"Depor{Shy}tivo",
		["Eintracht"] = $"Ein{Shy}tracht",
		["Frankfurt"] = $"Frank{Shy}furt",
		["Hamburger"] = $"Ham{Shy}burger",
		["Liverpool"] = $"Liver{Shy}pool",
		["Marseille"] = $"Mar{Shy}seille",
		["Newcastle"] = $"New{Shy}castle",
		["Stuttgart"] = $"Stutt{Shy}gart",
		["Tottenham"] = $"Totten{Shy}ham",
		["Vallecano"] = $"Valle{Shy}cano",
		["Wanderers"] = $"Wan{Shy}derers",
		["Wolfsburg"] = $"Wolfs{Shy}burg",
		["Argentina"] = $"Argen{Shy}tina",
		["Australia"] = $"Austra{Shy}lia",
		["Azerbaijan"] = $"Azer{Shy}baijan",
		["Bangladesh"] = $"Bangla{Shy}desh",
		["Bournemouth"] = $"Bourne{Shy}mouth",
		["Caledonia"] = $"Cale{Shy}donia",
		["Czechoslovakia"] = $"Czecho{Shy}slovakia",
		["Dominican"] = $"Domin{Shy}ican",
		["Equatorial"] = $"Equa{Shy}torial",
		["Fiorentina"] = $"Fioren{Shy}tina",
		["Gibraltar"] = $"Gibral{Shy}tar",
		["Grenadines"] = $"Grena{Shy}dines",
		["Guatemala"] = $"Guate{Shy}mala",
		["Heidenheim"] = $"Heiden{Shy}heim",
		["Herzegovina"] = $"Herze{Shy}govina",
		["Hoffenheim"] = $"Hoffen{Shy}heim",
		["Indonesia"] = $"Indo{Shy}nesia",
		["Kazakhstan"] = $"Kazakh{Shy}stan",
		["Kyrgyzstan"] = $"Kyrgyz{Shy}stan",
		["Leverkusen"] = $"Lever{Shy}kusen",
		["Liechtenstein"] = $"Liechten{Shy}stein",
		["Lithuania"] = $"Lithu{Shy}ania",
		["Luxembourg"] = $"Luxem{Shy}bourg",
		["Macedonia"] = $"Mace{Shy}donia",
		["Madagascar"] = $"Mada{Shy}gascar",
		["Manchester"] = $"Man{Shy}chester",
		["Mauritania"] = $"Mauri{Shy}tania",
		["Mauritius"] = $"Mauri{Shy}tius",
		["Mönchengladbach"] = $"Mönchen{Shy}gladbach",
		["Montenegro"] = $"Monte{Shy}negro",
		["Montserrat"] = $"Mont{Shy}serrat",
		["Mozambique"] = $"Mozam{Shy}bique",
		["Netherlands"] = $"Nether{Shy}lands",
		["Nicaragua"] = $"Nica{Shy}ragua",
		["Nottingham"] = $"Notting{Shy}ham",
		["Palestine"] = $"Pales{Shy}tine",
		["Philippines"] = $"Philip{Shy}pines",
		["Seychelles"] = $"Sey{Shy}chelles",
		["Singapore"] = $"Singa{Shy}pore",
		["Strasbourg"] = $"Stras{Shy}bourg",
		["Sunderland"] = $"Sunder{Shy}land",
		["Swaziland"] = $"Swazi{Shy}land",
		["Switzerland"] = $"Switzer{Shy}land",
		["Tajikistan"] = $"Tajik{Shy}istan",
		["Turkmenistan"] = $"Turkmen{Shy}istan",
		["Uzbekistan"] = $"Uzbek{Shy}istan",
		["Venezuela"] = $"Vene{Shy}zuela",
		["Villarreal"] = $"Villar{Shy}real",
		["Wolverhampton"] = $"Wolver{Shy}hampton",
		["Yugoslavia"] = $"Yugo{Shy}slavia",
	};

	/// <summary>Returns the name with soft hyphens in any known long word; everything else passes through unchanged.</summary>
	public static string Soften(string name)
	{
		if (name.Length < 9) { return name; }

		return string.Join(' ', name.Split(' ').Select(w => Words.GetValueOrDefault(w, w)));
	}
}
