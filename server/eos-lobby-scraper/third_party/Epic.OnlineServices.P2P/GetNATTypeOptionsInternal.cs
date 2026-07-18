using System;

namespace Epic.OnlineServices.P2P;

internal struct GetNATTypeOptionsInternal : ISettable<GetNATTypeOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetNATTypeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
