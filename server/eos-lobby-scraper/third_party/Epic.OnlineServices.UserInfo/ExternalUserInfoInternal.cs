using System;

namespace Epic.OnlineServices.UserInfo;

internal struct ExternalUserInfoInternal : IGettable<ExternalUserInfo>
{
	private int m_ApiVersion;

	private ExternalAccountType m_AccountType;

	private IntPtr m_AccountId;

	private IntPtr m_DisplayName;

	private IntPtr m_DisplayNameSanitized;

	public void Get(out ExternalUserInfo other)
	{
		other = default(ExternalUserInfo);
		other.AccountType = m_AccountType;
		Helper.Get(m_AccountId, out Utf8String to);
		other.AccountId = to;
		Helper.Get(m_DisplayName, out Utf8String to2);
		other.DisplayName = to2;
		Helper.Get(m_DisplayNameSanitized, out Utf8String to3);
		other.DisplayNameSanitized = to3;
	}
}
