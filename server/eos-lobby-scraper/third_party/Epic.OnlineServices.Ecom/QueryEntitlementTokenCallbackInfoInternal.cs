using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryEntitlementTokenCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryEntitlementTokenCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_EntitlementToken;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryEntitlementTokenCallbackInfo other)
	{
		other = default(QueryEntitlementTokenCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_EntitlementToken, out Utf8String to3);
		other.EntitlementToken = to3;
	}
}
