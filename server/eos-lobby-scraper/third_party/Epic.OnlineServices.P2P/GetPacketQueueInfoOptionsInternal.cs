using System;

namespace Epic.OnlineServices.P2P;

internal struct GetPacketQueueInfoOptionsInternal : ISettable<GetPacketQueueInfoOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetPacketQueueInfoOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
