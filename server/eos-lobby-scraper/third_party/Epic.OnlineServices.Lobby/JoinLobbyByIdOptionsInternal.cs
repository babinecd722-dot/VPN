using System;

namespace Epic.OnlineServices.Lobby;

internal struct JoinLobbyByIdOptionsInternal : ISettable<JoinLobbyByIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId;

	private IntPtr m_LocalUserId;

	private int m_PresenceEnabled;

	private IntPtr m_LocalRTCOptions;

	private int m_CrossplayOptOut;

	private LobbyRTCRoomJoinActionType m_RTCRoomJoinActionType;

	public void Set(ref JoinLobbyByIdOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set(other.LobbyId, ref m_LobbyId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.PresenceEnabled, ref m_PresenceEnabled);
		Helper.Set<LocalRTCOptions, LocalRTCOptionsInternal>(other.LocalRTCOptions, ref m_LocalRTCOptions);
		Helper.Set(other.CrossplayOptOut, ref m_CrossplayOptOut);
		m_RTCRoomJoinActionType = other.RTCRoomJoinActionType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyId);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_LocalRTCOptions);
	}
}
