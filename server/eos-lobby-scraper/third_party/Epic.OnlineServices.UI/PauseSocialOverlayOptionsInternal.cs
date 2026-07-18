using System;

namespace Epic.OnlineServices.UI;

internal struct PauseSocialOverlayOptionsInternal : ISettable<PauseSocialOverlayOptions>, IDisposable
{
	private int m_ApiVersion;

	private int m_IsPaused;

	public void Set(ref PauseSocialOverlayOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.IsPaused, ref m_IsPaused);
	}

	public void Dispose()
	{
	}
}
