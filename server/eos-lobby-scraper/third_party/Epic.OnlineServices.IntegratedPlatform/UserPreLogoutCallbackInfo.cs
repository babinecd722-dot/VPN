namespace Epic.OnlineServices.IntegratedPlatform;

public struct UserPreLogoutCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public Utf8String PlatformType { get; set; }

	public Utf8String LocalPlatformUserId { get; set; }

	public EpicAccountId AccountId { get; set; }

	public ProductUserId ProductUserId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
