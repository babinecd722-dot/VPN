using System;

namespace Epic.OnlineServices.Sanctions;

internal struct QueryActivePlayerSanctionsCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryActivePlayerSanctionsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_TargetUserId;

	private IntPtr m_LocalUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryActivePlayerSanctionsCallbackInfo other)
	{
		other = default(QueryActivePlayerSanctionsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_TargetUserId, out ProductUserId to2);
		other.TargetUserId = to2;
		Helper.Get(m_LocalUserId, out ProductUserId to3);
		other.LocalUserId = to3;
	}
}
