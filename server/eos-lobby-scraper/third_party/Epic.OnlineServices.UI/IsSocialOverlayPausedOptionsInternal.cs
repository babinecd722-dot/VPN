using System;

namespace Epic.OnlineServices.UI;

internal struct IsSocialOverlayPausedOptionsInternal : ISettable<IsSocialOverlayPausedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref IsSocialOverlayPausedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
