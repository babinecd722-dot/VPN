using System;

namespace Epic.OnlineServices.Lobby;

internal struct JoinLobbyOptionsInternal : ISettable<JoinLobbyOptions>, IDisposable
{
	public JoinLobbyOptionsInternal()
	{
	}

	private int m_ApiVersion = default;

	internal int ApiVersion => m_ApiVersion;

	private IntPtr m_LobbyDetailsHandle = default;

	private IntPtr m_LocalUserId = default;

	private int m_PresenceEnabled = default;

	private IntPtr m_LocalRTCOptions = default;

	private int m_CrossplayOptOut = default;

	private LobbyRTCRoomJoinActionType m_RTCRoomJoinActionType = default;

	public void Set(ref JoinLobbyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
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
