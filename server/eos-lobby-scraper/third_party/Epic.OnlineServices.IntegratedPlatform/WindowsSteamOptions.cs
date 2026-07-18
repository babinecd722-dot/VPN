namespace Epic.OnlineServices.IntegratedPlatform;

public struct WindowsSteamOptions
{
	public Utf8String Type { get; set; }

	public IntegratedPlatformManagementFlags Flags { get; set; }

	public WindowsSteamOptionsInitOptions? InitOptions { get; set; }
}
