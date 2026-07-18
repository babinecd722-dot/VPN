namespace Epic.OnlineServices.Auth;

public struct LoginStatusChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public EpicAccountId LocalUserId { get; set; }

	public LoginStatus PrevStatus { get; set; }

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
