using System;

namespace Epic.OnlineServices.UserInfo;

internal struct QueryUserInfoByExternalAccountOptionsInternal : ISettable<QueryUserInfoByExternalAccountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ExternalAccountId;

	private ExternalAccountType m_AccountType;

	public void Set(ref QueryUserInfoByExternalAccountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.ExternalAccountId, ref m_ExternalAccountId);
		m_AccountType = other.AccountType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ExternalAccountId);
	}
}
