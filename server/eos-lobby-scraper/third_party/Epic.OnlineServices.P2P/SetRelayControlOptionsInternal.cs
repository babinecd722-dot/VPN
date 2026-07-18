using System;

namespace Epic.OnlineServices.P2P;

internal struct SetRelayControlOptionsInternal : ISettable<SetRelayControlOptions>, IDisposable
{
	private int m_ApiVersion;

	private RelayControl m_RelayControl;

	public void Set(ref SetRelayControlOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_RelayControl = other.RelayControl;
	}

	public void Dispose()
	{
	}
}
