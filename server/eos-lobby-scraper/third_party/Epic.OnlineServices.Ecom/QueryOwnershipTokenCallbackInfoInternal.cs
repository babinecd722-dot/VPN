using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryOwnershipTokenCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryOwnershipTokenCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_OwnershipToken;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryOwnershipTokenCallbackInfo other)
	{
		other = default(QueryOwnershipTokenCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_OwnershipToken, out Utf8String to3);
		other.OwnershipToken = to3;
	}
}
