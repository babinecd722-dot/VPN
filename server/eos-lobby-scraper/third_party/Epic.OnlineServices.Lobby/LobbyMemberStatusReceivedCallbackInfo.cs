namespace Epic.OnlineServices.Lobby;

public struct LobbyMemberStatusReceivedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public Utf8String LobbyId { get; set; }

	public ProductUserId TargetUserId { get; set; }

	public LobbyMemberStatus CurrentStatus { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
