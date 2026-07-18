using System;

namespace Epic.OnlineServices.IntegratedPlatform;

public struct Options
{
	public Utf8String Type { get; set; }

	public IntegratedPlatformManagementFlags Flags { get; set; }

	public IntPtr InitOptions { get; set; }
}
