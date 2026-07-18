using System;

namespace Epic.OnlineServices.AntiCheatClient;

public struct OnMessageToServerCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

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
