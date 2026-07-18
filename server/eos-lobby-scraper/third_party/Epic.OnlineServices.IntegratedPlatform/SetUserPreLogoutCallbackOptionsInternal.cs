using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct SetUserPreLogoutCallbackOptionsInternal : ISettable<SetUserPreLogoutCallbackOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref SetUserPreLogoutCallbackOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
