using System;

namespace Epic.OnlineServices.UI;

public struct MemoryMonitorCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public IntPtr SystemMemoryMonitorReport { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
