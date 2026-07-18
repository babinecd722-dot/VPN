using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct OnMessageToClientCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public IntPtr ClientHandle { get; set; }

	public ArraySegment<byte> MessageData { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
