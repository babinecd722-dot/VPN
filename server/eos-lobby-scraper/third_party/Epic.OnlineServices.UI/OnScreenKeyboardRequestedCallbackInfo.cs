namespace Epic.OnlineServices.UI;

public struct OnScreenKeyboardRequestedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public OnScreenKeyboardType Type { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
