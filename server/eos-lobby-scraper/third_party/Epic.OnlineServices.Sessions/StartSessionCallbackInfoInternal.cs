using System;

namespace Epic.OnlineServices.Sessions;

internal struct StartSessionCallbackInfoInternal : ICallbackInfoInternal, IGettable<StartSessionCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out StartSessionCallbackInfo other)
	{
		other = default(StartSessionCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
