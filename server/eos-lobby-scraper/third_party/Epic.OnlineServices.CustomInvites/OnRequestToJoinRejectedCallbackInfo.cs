namespace Epic.OnlineServices.CustomInvites;

public struct OnRequestToJoinRejectedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId TargetUserId { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
