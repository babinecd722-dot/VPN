namespace Epic.OnlineServices.KWS;

public struct PermissionsUpdateReceivedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String KWSUserId { get; set; }

	public Utf8String DateOfBirth { get; set; }

	public bool IsMinor { get; set; }

	public Utf8String ParentEmail { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
