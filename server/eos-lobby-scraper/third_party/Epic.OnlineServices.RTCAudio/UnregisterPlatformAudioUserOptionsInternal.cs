using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct UnregisterPlatformAudioUserOptionsInternal : ISettable<UnregisterPlatformAudioUserOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	public void Set(ref UnregisterPlatformAudioUserOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.UserId, ref m_UserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UserId);
	}
}
