using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct OnClientActionRequiredCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public IntPtr ClientHandle { get; set; }

	public AntiCheatCommonClientAction ClientAction { get; set; }

	public AntiCheatCommonClientActionReason ActionReasonCode { get; set; }

	public Utf8String ActionReasonDetailsString { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
