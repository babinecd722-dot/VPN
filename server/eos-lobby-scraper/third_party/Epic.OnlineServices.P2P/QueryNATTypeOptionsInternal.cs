using System;

namespace Epic.OnlineServices.P2P;

internal struct QueryNATTypeOptionsInternal : ISettable<QueryNATTypeOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref QueryNATTypeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
