using System;

namespace Epic.OnlineServices.Sessions;

internal struct DestroySessionCallbackInfoInternal : ICallbackInfoInternal, IGettable<DestroySessionCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out DestroySessionCallbackInfo other)
	{
		other = default(DestroySessionCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
