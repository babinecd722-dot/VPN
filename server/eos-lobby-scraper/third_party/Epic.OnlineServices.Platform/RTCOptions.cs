using System;

namespace Epic.OnlineServices.Platform;

public struct RTCOptions
{
	public IntPtr PlatformSpecificOptions { get; set; }

	public RTCBackgroundMode BackgroundMode { get; set; }

	public IntPtr Reserved { get; set; }
}
