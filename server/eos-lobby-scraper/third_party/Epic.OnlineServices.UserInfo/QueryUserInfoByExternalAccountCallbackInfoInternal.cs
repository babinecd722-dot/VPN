using System;

namespace Epic.OnlineServices.UserInfo;

internal struct QueryUserInfoByExternalAccountCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryUserInfoByExternalAccountCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_ExternalAccountId;

	private ExternalAccountType m_AccountType;

	private IntPtr m_TargetUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryUserInfoByExternalAccountCallbackInfo other)
	{
		other = default(QueryUserInfoByExternalAccountCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_ExternalAccountId, out Utf8String to3);
		other.ExternalAccountId = to3;
		other.AccountType = m_AccountType;
		Helper.Get(m_TargetUserId, out EpicAccountId to4);
		other.TargetUserId = to4;
	}
}
