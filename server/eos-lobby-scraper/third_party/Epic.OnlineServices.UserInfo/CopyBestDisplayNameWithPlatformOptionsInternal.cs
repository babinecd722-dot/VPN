using System;

namespace Epic.OnlineServices.UserInfo;

internal struct CopyBestDisplayNameWithPlatformOptionsInternal : ISettable<CopyBestDisplayNameWithPlatformOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private uint m_TargetPlatformType;

	public void Set(ref CopyBestDisplayNameWithPlatformOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_TargetPlatformType = other.TargetPlatformType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
