namespace Epic.OnlineServices.P2P;

internal struct PacketQueueInfoInternal : IGettable<PacketQueueInfo>
{
	private ulong m_IncomingPacketQueueMaxSizeBytes;

	private ulong m_IncomingPacketQueueCurrentSizeBytes;

	private ulong m_IncomingPacketQueueCurrentPacketCount;

	private ulong m_OutgoingPacketQueueMaxSizeBytes;

	private ulong m_OutgoingPacketQueueCurrentSizeBytes;

	private ulong m_OutgoingPacketQueueCurrentPacketCount;

	public void Get(out PacketQueueInfo other)
	{
		other = default(PacketQueueInfo);
		other.IncomingPacketQueueMaxSizeBytes = m_IncomingPacketQueueMaxSizeBytes;
		other.IncomingPacketQueueCurrentSizeBytes = m_IncomingPacketQueueCurrentSizeBytes;
		other.IncomingPacketQueueCurrentPacketCount = m_IncomingPacketQueueCurrentPacketCount;
		other.OutgoingPacketQueueMaxSizeBytes = m_OutgoingPacketQueueMaxSizeBytes;
		other.OutgoingPacketQueueCurrentSizeBytes = m_OutgoingPacketQueueCurrentSizeBytes;
		other.OutgoingPacketQueueCurrentPacketCount = m_OutgoingPacketQueueCurrentPacketCount;
	}
}
