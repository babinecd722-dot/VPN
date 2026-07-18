namespace Epic.OnlineServices.Lobby;

public struct JoinLobbyAcceptedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public ulong UiEventId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
