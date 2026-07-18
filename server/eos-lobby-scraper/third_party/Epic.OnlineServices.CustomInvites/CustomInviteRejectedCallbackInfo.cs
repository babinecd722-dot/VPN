namespace Epic.OnlineServices.CustomInvites;

public struct CustomInviteRejectedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId TargetUserId { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String CustomInviteId { get; set; }

	public Utf8String Payload { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
