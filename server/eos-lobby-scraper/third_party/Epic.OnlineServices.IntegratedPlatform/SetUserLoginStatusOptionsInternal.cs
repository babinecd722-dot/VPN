using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct SetUserLoginStatusOptionsInternal : ISettable<SetUserLoginStatusOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlatformType;

	private IntPtr m_LocalPlatformUserId;

	private LoginStatus m_CurrentLoginStatus;

	public void Set(ref SetUserLoginStatusOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.PlatformType, ref m_PlatformType);
		Helper.Set(other.LocalPlatformUserId, ref m_LocalPlatformUserId);
		m_CurrentLoginStatus = other.CurrentLoginStatus;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlatformType);
		Helper.Dispose(ref m_LocalPlatformUserId);
	}
}
