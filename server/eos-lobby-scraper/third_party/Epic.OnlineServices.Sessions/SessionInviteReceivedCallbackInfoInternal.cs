using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionInviteReceivedCallbackInfoInternal : ICallbackInfoInternal, IGettable<SessionInviteReceivedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private IntPtr m_InviteId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SessionInviteReceivedCallbackInfo other)
	{
		other = default(SessionInviteReceivedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetUserId, out ProductUserId to3);
		other.TargetUserId = to3;
		Helper.Get(m_InviteId, out Utf8String to4);
		other.InviteId = to4;
	}
}
