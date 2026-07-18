using System;

namespace Epic.OnlineServices.TitleStorage;

internal struct DeleteCacheCallbackInfoInternal : ICallbackInfoInternal, IGettable<DeleteCacheCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out DeleteCacheCallbackInfo other)
	{
		other = default(DeleteCacheCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
	}
}
