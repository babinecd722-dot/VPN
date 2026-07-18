using System;

namespace Epic.OnlineServices.Lobby;

internal struct JoinLobbyOptionsInternal : ISettable<JoinLobbyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyDetailsHandle;

	private IntPtr m_LocalUserId;

	private int m_PresenceEnabled;

	private IntPtr m_LocalRTCOptions;

	private int m_CrossplayOptOut;

	private LobbyRTCRoomJoinActionType m_RTCRoomJoinActionType;

	public void Set(ref JoinLobbyOptions other)
	{
		Dispose();
		m_ApiVersion = 5;
		Helper.Set((Handle)other.LobbyDetailsHandle, ref m_LobbyDetailsHandle);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.PresenceEnabled, ref m_PresenceEnabled);
		Helper.Set<LocalRTCOptions, LocalRTCOptionsInternal>(other.LocalRTCOptions, ref m_LocalRTCOptions);
		Helper.Set(other.CrossplayOptOut, ref m_CrossplayOptOut);
		m_RTCRoomJoinActionType = other.RTCRoomJoinActionType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyDetailsHandle);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_LocalRTCOptions);
	}
}
