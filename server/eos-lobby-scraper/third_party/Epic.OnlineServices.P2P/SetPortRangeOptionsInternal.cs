using System;

namespace Epic.OnlineServices.P2P;

internal struct SetPortRangeOptionsInternal : ISettable<SetPortRangeOptions>, IDisposable
{
	private int m_ApiVersion;

	private ushort m_Port;

	private ushort m_MaxAdditionalPortsToTry;

	public void Set(ref SetPortRangeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_Port = other.Port;
		m_MaxAdditionalPortsToTry = other.MaxAdditionalPortsToTry;
	}

	public void Dispose()
	{
	}
}
