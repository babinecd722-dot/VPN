namespace Epic.OnlineServices.RTCAudio;

public struct AudioDevicesChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
