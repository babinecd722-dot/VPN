using System;

namespace Epic.OnlineServices.Platform;

internal struct WindowsRTCOptionsPlatformSpecificOptionsInternal : ISettable<WindowsRTCOptionsPlatformSpecificOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_XAudio29DllPath;

	public void Set(ref WindowsRTCOptionsPlatformSpecificOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.XAudio29DllPath, ref m_XAudio29DllPath);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_XAudio29DllPath);
	}
}
