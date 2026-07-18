namespace Epic.OnlineServices.P2P;

public struct OnPeerConnectionEstablishedInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ProductUserId LocalUserId { get; set; }

	public ProductUserId RemoteUserId { get; set; }

	public SocketId? SocketId { get; set; }

	public ConnectionEstablishedType ConnectionType { get; set; }

	public NetworkConnectionType NetworkType { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
