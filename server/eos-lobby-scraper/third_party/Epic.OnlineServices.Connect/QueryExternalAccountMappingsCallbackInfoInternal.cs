using System;

namespace Epic.OnlineServices.Connect;

internal struct QueryExternalAccountMappingsCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryExternalAccountMappingsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryExternalAccountMappingsCallbackInfo other)
	{
		other = default(QueryExternalAccountMappingsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
	}
}
