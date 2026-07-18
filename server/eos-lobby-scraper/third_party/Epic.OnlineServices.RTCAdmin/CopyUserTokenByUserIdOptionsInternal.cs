using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct CopyUserTokenByUserIdOptionsInternal : ISettable<CopyUserTokenByUserIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private uint m_QueryId;

	public void Set(ref CopyUserTokenByUserIdOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_QueryId = other.QueryId;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
	}
}
