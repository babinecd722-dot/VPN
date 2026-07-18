using System;

namespace Epic.OnlineServices.Sessions;

internal struct LeaveSessionRequestedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LeaveSessionRequestedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_SessionName;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LeaveSessionRequestedCallbackInfo other)
	{
		other = default(LeaveSessionRequestedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_SessionName, out Utf8String to3);
		other.SessionName = to3;
	}
}
