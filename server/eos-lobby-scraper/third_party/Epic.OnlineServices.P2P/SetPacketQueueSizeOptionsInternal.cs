using System;

namespace Epic.OnlineServices.P2P;

internal struct SetPacketQueueSizeOptionsInternal : ISettable<SetPacketQueueSizeOptions>, IDisposable
{
	private int m_ApiVersion;

	private ulong m_IncomingPacketQueueMaxSizeBytes;

	private ulong m_OutgoingPacketQueueMaxSizeBytes;

	public void Set(ref SetPacketQueueSizeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_IncomingPacketQueueMaxSizeBytes = other.IncomingPacketQueueMaxSizeBytes;
		m_OutgoingPacketQueueMaxSizeBytes = other.OutgoingPacketQueueMaxSizeBytes;
	}

	public void Dispose()
	{
	}
}
