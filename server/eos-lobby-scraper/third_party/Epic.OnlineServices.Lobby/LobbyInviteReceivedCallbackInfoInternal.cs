using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyInviteReceivedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LobbyInviteReceivedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_InviteId;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LobbyInviteReceivedCallbackInfo other)
	{
		other = default(LobbyInviteReceivedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_InviteId, out Utf8String to2);
		other.InviteId = to2;
		Helper.Get(m_LocalUserId, out ProductUserId to3);
		other.LocalUserId = to3;
		Helper.Get(m_TargetUserId, out ProductUserId to4);
		other.TargetUserId = to4;
	}
}
