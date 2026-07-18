namespace Epic.OnlineServices.Lobby;

public struct RejectInviteCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public Utf8String InviteId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
