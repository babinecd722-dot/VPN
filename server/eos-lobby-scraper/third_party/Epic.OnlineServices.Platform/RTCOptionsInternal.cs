using System;

namespace Epic.OnlineServices.Platform;

internal struct RTCOptionsInternal : ISettable<RTCOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlatformSpecificOptions;

	private RTCBackgroundMode m_BackgroundMode;

	private IntPtr m_Reserved;

	public void Set(ref RTCOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		m_PlatformSpecificOptions = other.PlatformSpecificOptions;
		m_BackgroundMode = other.BackgroundMode;
		m_Reserved = other.Reserved;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlatformSpecificOptions);
		Helper.Dispose(ref m_Reserved);
	}
}
