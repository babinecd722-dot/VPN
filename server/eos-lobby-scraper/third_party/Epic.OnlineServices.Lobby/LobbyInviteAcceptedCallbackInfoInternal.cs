using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyInviteAcceptedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LobbyInviteAcceptedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_InviteId;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private IntPtr m_LobbyId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LobbyInviteAcceptedCallbackInfo other)
	{
		other = default(LobbyInviteAcceptedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_InviteId, out Utf8String to2);
		other.InviteId = to2;
		Helper.Get(m_LocalUserId, out ProductUserId to3);
		other.LocalUserId = to3;
		Helper.Get(m_TargetUserId, out ProductUserId to4);
		other.TargetUserId = to4;
		Helper.Get(m_LobbyId, out Utf8String to5);
		other.LobbyId = to5;
	}
}
