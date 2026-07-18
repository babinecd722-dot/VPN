using System;

namespace Epic.OnlineServices.Auth;

internal struct DeletePersistentAuthCallbackInfoInternal : ICallbackInfoInternal, IGettable<DeletePersistentAuthCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out DeletePersistentAuthCallbackInfo other)
	{
		other = default(DeletePersistentAuthCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
