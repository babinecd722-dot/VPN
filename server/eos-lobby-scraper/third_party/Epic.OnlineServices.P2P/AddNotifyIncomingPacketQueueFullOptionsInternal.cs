using System;

namespace Epic.OnlineServices.P2P;

internal struct AddNotifyIncomingPacketQueueFullOptionsInternal : ISettable<AddNotifyIncomingPacketQueueFullOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyIncomingPacketQueueFullOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
