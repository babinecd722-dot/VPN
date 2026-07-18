using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyMemberStatusReceivedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LobbyMemberStatusReceivedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LobbyId;

	private IntPtr m_TargetUserId;

	private LobbyMemberStatus m_CurrentStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LobbyMemberStatusReceivedCallbackInfo other)
	{
		other = default(LobbyMemberStatusReceivedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LobbyId, out Utf8String to2);
		other.LobbyId = to2;
		Helper.Get(m_TargetUserId, out ProductUserId to3);
		other.TargetUserId = to3;
		other.CurrentStatus = m_CurrentStatus;
	}
}
