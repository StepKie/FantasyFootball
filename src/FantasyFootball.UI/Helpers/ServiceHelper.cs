namespace FantasyFootball.Helpers;

public static class ServiceHelper
{
	public static TService? GetService<TService>() => IPlatformApplication.Current!.Services.GetService<TService>();
}
