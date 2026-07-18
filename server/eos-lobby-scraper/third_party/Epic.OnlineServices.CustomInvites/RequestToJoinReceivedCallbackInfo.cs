namespace Epic.OnlineServices.CustomInvites;

public struct RequestToJoinReceivedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId FromUserId { get; set; }

	public ProductUserId ToUserId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
