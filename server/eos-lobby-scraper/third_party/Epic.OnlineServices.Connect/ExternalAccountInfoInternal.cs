using System;

namespace Epic.OnlineServices.Connect;

internal struct ExternalAccountInfoInternal : IGettable<ExternalAccountInfo>
{
	private int m_ApiVersion;

	private IntPtr m_ProductUserId;

	private IntPtr m_DisplayName;

	private IntPtr m_AccountId;

	private ExternalAccountType m_AccountIdType;

	private long m_LastLoginTime;

	public void Get(out ExternalAccountInfo other)
	{
		other = default(ExternalAccountInfo);
		Helper.Get(m_ProductUserId, out ProductUserId to);
		other.ProductUserId = to;
		Helper.Get(m_DisplayName, out Utf8String to2);
		other.DisplayName = to2;
		Helper.Get(m_AccountId, out Utf8String to3);
		other.AccountId = to3;
		other.AccountIdType = m_AccountIdType;
		Helper.Get(m_LastLoginTime, out DateTimeOffset? to4);
		other.LastLoginTime = to4;
	}
}
