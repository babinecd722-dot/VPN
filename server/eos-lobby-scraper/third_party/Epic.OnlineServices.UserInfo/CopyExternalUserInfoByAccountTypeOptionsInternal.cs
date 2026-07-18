using System;

namespace Epic.OnlineServices.UserInfo;

internal struct CopyExternalUserInfoByAccountTypeOptionsInternal : ISettable<CopyExternalUserInfoByAccountTypeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private ExternalAccountType m_AccountType;

	public void Set(ref CopyExternalUserInfoByAccountTypeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_AccountType = other.AccountType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
