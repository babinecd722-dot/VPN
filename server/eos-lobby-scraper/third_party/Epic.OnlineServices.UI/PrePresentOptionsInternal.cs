using System;

namespace Epic.OnlineServices.UI;

internal struct PrePresentOptionsInternal : ISettable<PrePresentOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlatformSpecificData;

	public void Set(ref PrePresentOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_PlatformSpecificData = other.PlatformSpecificData;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlatformSpecificData);
	}
}
