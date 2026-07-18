using System;

namespace Epic.OnlineServices.Connect;

internal struct CopyProductUserExternalAccountByAccountTypeOptionsInternal : ISettable<CopyProductUserExternalAccountByAccountTypeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private ExternalAccountType m_AccountIdType;

	public void Set(ref CopyProductUserExternalAccountByAccountTypeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_AccountIdType = other.AccountIdType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
	}
}
