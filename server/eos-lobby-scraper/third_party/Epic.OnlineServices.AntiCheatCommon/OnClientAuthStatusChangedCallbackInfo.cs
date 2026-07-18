using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct OnClientAuthStatusChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public IntPtr ClientHandle { get; set; }

	public AntiCheatCommonClientAuthStatus ClientAuthStatus { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
