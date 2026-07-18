using System;

namespace Epic.OnlineServices.Lobby;

internal struct JoinRTCRoomOptionsInternal : ISettable<JoinRTCRoomOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId;

	private IntPtr m_LocalUserId;

	private IntPtr m_LocalRTCOptions;

	public void Set(ref JoinRTCRoomOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.LobbyId, ref m_LobbyId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set<LocalRTCOptions, LocalRTCOptionsInternal>(other.LocalRTCOptions, ref m_LocalRTCOptions);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyId);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_LocalRTCOptions);
	}
}
