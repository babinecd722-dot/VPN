using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct CreateIntegratedPlatformOptionsContainerOptionsInternal : ISettable<CreateIntegratedPlatformOptionsContainerOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref CreateIntegratedPlatformOptionsContainerOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
