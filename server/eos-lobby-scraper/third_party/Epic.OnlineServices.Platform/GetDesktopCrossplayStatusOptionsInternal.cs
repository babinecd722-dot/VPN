using System;

namespace Epic.OnlineServices.Platform;

internal struct GetDesktopCrossplayStatusOptionsInternal : ISettable<GetDesktopCrossplayStatusOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetDesktopCrossplayStatusOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
