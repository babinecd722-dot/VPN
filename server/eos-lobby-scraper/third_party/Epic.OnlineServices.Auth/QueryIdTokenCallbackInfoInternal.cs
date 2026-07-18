using System;

namespace Epic.OnlineServices.Auth;

internal struct QueryIdTokenCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryIdTokenCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetAccountId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryIdTokenCallbackInfo other)
	{
		other = default(QueryIdTokenCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetAccountId, out EpicAccountId to3);
		other.TargetAccountId = to3;
	}
}
