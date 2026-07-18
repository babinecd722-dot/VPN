using System;

namespace Epic.OnlineServices.Platform;

public struct AndroidInitializeOptionsSystemInitializeOptions
{
	public IntPtr Reserved { get; set; }

	public Utf8String OptionalInternalDirectory { get; set; }

	public Utf8String OptionalExternalDirectory { get; set; }
}
