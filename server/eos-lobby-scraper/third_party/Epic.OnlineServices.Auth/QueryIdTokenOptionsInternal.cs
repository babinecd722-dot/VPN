using System;

namespace Epic.OnlineServices.Auth;

internal struct QueryIdTokenOptionsInternal : ISettable<QueryIdTokenOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetAccountId;

	public void Set(ref QueryIdTokenOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetAccountId, ref m_TargetAccountId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetAccountId);
	}
}
