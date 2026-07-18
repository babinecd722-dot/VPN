using System;

namespace Epic.OnlineServices.UserInfo;

internal struct CopyExternalUserInfoByIndexOptionsInternal : ISettable<CopyExternalUserInfoByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private uint m_Index;

	public void Set(ref CopyExternalUserInfoByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_Index = other.Index;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
