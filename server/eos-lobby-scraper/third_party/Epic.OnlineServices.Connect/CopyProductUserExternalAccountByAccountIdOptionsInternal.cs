using System;

namespace Epic.OnlineServices.Connect;

internal struct CopyProductUserExternalAccountByAccountIdOptionsInternal : ISettable<CopyProductUserExternalAccountByAccountIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private IntPtr m_AccountId;

	public void Set(ref CopyProductUserExternalAccountByAccountIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set(other.AccountId, ref m_AccountId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
		Helper.Dispose(ref m_AccountId);
	}
}
