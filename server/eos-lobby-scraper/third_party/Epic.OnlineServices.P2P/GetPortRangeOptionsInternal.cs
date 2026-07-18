using System;

namespace Epic.OnlineServices.P2P;

internal struct GetPortRangeOptionsInternal : ISettable<GetPortRangeOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetPortRangeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
