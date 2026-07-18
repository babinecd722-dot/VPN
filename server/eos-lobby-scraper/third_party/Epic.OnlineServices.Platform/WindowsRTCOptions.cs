using System;

namespace Epic.OnlineServices.Platform;

public struct WindowsRTCOptions
{
	public WindowsRTCOptionsPlatformSpecificOptions? PlatformSpecificOptions { get; set; }

	public RTCBackgroundMode BackgroundMode { get; set; }

	public IntPtr Reserved { get; set; }
}
