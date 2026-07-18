using System;

namespace Epic.OnlineServices.Auth;

internal struct CopyUserAuthTokenOptionsInternal : ISettable<CopyUserAuthTokenOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref CopyUserAuthTokenOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
