namespace Epic.OnlineServices.IntegratedPlatform;

public struct WindowsSteamOptionsInitOptions
{
	public Utf8String OverrideLibraryPath { get; set; }

	public uint SteamMajorVersion { get; set; }

	public uint SteamMinorVersion { get; set; }

	public Utf8String SteamApiInterfaceVersionsArray { get; set; }

	public uint SteamApiInterfaceVersionsArrayBytes { get; set; }
}
