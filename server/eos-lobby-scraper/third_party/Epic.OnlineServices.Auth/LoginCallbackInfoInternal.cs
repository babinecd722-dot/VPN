using System;

namespace Epic.OnlineServices.Auth;

internal struct LoginCallbackInfoInternal : ICallbackInfoInternal, IGettable<LoginCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_PinGrantInfo;

	private IntPtr m_ContinuanceToken;

	private IntPtr m_AccountFeatureRestrictedInfo_DEPRECATED;

	private IntPtr m_SelectedAccountId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LoginCallbackInfo other)
	{
		other = default(LoginCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get<PinGrantInfoInternal, PinGrantInfo>(m_PinGrantInfo, out PinGrantInfo? to3);
		other.PinGrantInfo = to3;
		Helper.Get(m_ContinuanceToken, out ContinuanceToken to4);
		other.ContinuanceToken = to4;
		Helper.Get<AccountFeatureRestrictedInfoInternal, AccountFeatureRestrictedInfo>(m_AccountFeatureRestrictedInfo_DEPRECATED, out AccountFeatureRestrictedInfo? to5);
		other.AccountFeatureRestrictedInfo_DEPRECATED = to5;
		Helper.Get(m_SelectedAccountId, out EpicAccountId to6);
		other.SelectedAccountId = to6;
	}
}
