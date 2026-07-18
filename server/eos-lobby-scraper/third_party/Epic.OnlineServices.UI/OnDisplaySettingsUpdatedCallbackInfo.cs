namespace Epic.OnlineServices.UI;

public struct OnDisplaySettingsUpdatedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public bool IsVisible { get; set; }

	public bool IsExclusiveInput { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
