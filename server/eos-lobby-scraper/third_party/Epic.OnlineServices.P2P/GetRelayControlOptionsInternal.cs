using System;

namespace Epic.OnlineServices.P2P;

internal struct GetRelayControlOptionsInternal : ISettable<GetRelayControlOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetRelayControlOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
