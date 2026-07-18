using System;

namespace Epic.OnlineServices.P2P;

internal struct OnIncomingPacketQueueFullInfoInternal : ICallbackInfoInternal, IGettable<OnIncomingPacketQueueFullInfo>
{
	private IntPtr m_ClientData;

	private ulong m_PacketQueueMaxSizeBytes;

	private ulong m_PacketQueueCurrentSizeBytes;

	private IntPtr m_OverflowPacketLocalUserId;

	private byte m_OverflowPacketChannel;

	private uint m_OverflowPacketSizeBytes;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnIncomingPacketQueueFullInfo other)
	{
		other = default(OnIncomingPacketQueueFullInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.PacketQueueMaxSizeBytes = m_PacketQueueMaxSizeBytes;
		other.PacketQueueCurrentSizeBytes = m_PacketQueueCurrentSizeBytes;
		Helper.Get(m_OverflowPacketLocalUserId, out ProductUserId to2);
		other.OverflowPacketLocalUserId = to2;
		other.OverflowPacketChannel = m_OverflowPacketChannel;
		other.OverflowPacketSizeBytes = m_OverflowPacketSizeBytes;
	}
}
