using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionInviteAcceptedCallbackInfoInternal : ICallbackInfoInternal, IGettable<SessionInviteAcceptedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_SessionId;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private IntPtr m_InviteId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SessionInviteAcceptedCallbackInfo other)
	{
		other = default(SessionInviteAcceptedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_SessionId, out Utf8String to2);
		other.SessionId = to2;
		Helper.Get(m_LocalUserId, out ProductUserId to3);
		other.LocalUserId = to3;
		Helper.Get(m_TargetUserId, out ProductUserId to4);
		other.TargetUserId = to4;
		Helper.Get(m_InviteId, out Utf8String to5);
		other.InviteId = to5;
	}
}
