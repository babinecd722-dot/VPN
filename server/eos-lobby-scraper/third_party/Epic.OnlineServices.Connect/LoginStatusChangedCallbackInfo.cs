namespace Epic.OnlineServices.Connect;

public struct LoginStatusChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public LoginStatus PreviousStatus { get; set; }

	public LoginStatus CurrentStatus { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
