using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct RegisterPlatformUserOptionsInternal : ISettable<RegisterPlatformUserOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlatformUserId;

	public void Set(ref RegisterPlatformUserOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.PlatformUserId, ref m_PlatformUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlatformUserId);
	}
}
