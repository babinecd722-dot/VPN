using System;

namespace Epic.OnlineServices.Platform;

internal struct WindowsRTCOptionsInternal : ISettable<WindowsRTCOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlatformSpecificOptions;

	private RTCBackgroundMode m_BackgroundMode;

	private IntPtr m_Reserved;

	public void Set(ref WindowsRTCOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set<WindowsRTCOptionsPlatformSpecificOptions, WindowsRTCOptionsPlatformSpecificOptionsInternal>(other.PlatformSpecificOptions, ref m_PlatformSpecificOptions);
		m_BackgroundMode = other.BackgroundMode;
		m_Reserved = other.Reserved;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlatformSpecificOptions);
		Helper.Dispose(ref m_Reserved);
	}
}
