namespace Epic.OnlineServices.CustomInvites;

public struct RequestToJoinResponseReceivedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId FromUserId { get; set; }

	public ProductUserId ToUserId { get; set; }

	public RequestToJoinResponse Response { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
