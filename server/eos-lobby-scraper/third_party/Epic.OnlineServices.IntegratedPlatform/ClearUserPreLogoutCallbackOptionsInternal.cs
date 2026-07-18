using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct ClearUserPreLogoutCallbackOptionsInternal : ISettable<ClearUserPreLogoutCallbackOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref ClearUserPreLogoutCallbackOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
