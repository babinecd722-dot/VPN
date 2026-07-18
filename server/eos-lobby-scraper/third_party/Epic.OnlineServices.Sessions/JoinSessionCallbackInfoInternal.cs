using System;

namespace Epic.OnlineServices.Sessions;

internal struct JoinSessionCallbackInfoInternal : ICallbackInfoInternal, IGettable<JoinSessionCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out JoinSessionCallbackInfo other)
	{
		other = default(JoinSessionCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
