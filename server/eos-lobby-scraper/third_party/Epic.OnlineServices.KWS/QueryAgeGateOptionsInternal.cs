using System;

namespace Epic.OnlineServices.KWS;

internal struct QueryAgeGateOptionsInternal : ISettable<QueryAgeGateOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref QueryAgeGateOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
