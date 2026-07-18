namespace Epic.OnlineServices.Lobby;

public struct RTCRoomConnectionChangedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public Utf8String LobbyId { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public bool IsConnected { get; set; }

	public Result DisconnectReason { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
