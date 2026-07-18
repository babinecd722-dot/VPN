namespace Epic.OnlineServices.Sessions;

public struct SessionInviteAcceptedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public Utf8String SessionId { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public ProductUserId TargetUserId { get; set; }

	public Utf8String InviteId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
