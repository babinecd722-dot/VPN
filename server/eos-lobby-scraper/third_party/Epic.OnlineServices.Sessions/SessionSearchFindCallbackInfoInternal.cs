using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionSearchFindCallbackInfoInternal : ICallbackInfoInternal, IGettable<SessionSearchFindCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SessionSearchFindCallbackInfo other)
	{
		other = default(SessionSearchFindCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
