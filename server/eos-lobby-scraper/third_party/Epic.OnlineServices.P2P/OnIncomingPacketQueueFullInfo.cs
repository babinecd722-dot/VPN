namespace Epic.OnlineServices.P2P;

public struct OnIncomingPacketQueueFullInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public ulong PacketQueueMaxSizeBytes { get; set; }

	public ulong PacketQueueCurrentSizeBytes { get; set; }

	public ProductUserId OverflowPacketLocalUserId { get; set; }

	public byte OverflowPacketChannel { get; set; }

	public uint OverflowPacketSizeBytes { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
