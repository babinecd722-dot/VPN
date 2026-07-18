using System;

namespace Epic.OnlineServices.Sessions;

internal struct EndSessionCallbackInfoInternal : ICallbackInfoInternal, IGettable<EndSessionCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out EndSessionCallbackInfo other)
	{
		other = default(EndSessionCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
