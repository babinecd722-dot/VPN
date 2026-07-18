namespace Epic.OnlineServices.IntegratedPlatform;

public struct UserLoginStatusChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public Utf8String PlatformType { get; set; }

	public Utf8String LocalPlatformUserId { get; set; }

	public EpicAccountId AccountId { get; set; }

	public ProductUserId ProductUserId { get; set; }

	public LoginStatus PreviousLoginStatus { get; set; }

	public LoginStatus CurrentLoginStatus { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
