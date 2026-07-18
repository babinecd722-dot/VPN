using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct FinalizeDeferredUserLogoutOptionsInternal : ISettable<FinalizeDeferredUserLogoutOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlatformType;

	private IntPtr m_LocalPlatformUserId;

	private LoginStatus m_ExpectedLoginStatus;

	public void Set(ref FinalizeDeferredUserLogoutOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.PlatformType, ref m_PlatformType);
		Helper.Set(other.LocalPlatformUserId, ref m_LocalPlatformUserId);
		m_ExpectedLoginStatus = other.ExpectedLoginStatus;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlatformType);
		Helper.Dispose(ref m_LocalPlatformUserId);
	}
}
