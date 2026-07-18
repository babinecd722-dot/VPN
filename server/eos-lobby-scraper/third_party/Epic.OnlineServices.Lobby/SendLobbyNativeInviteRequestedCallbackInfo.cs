namespace Epic.OnlineServices.Lobby;

public struct SendLobbyNativeInviteRequestedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ulong UiEventId { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public Utf8String TargetNativeAccountType { get; set; }

	public Utf8String TargetUserNativeAccountId { get; set; }

	public Utf8String LobbyId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
