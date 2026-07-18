using System;

namespace Epic.OnlineServices.Lobby;

internal struct IsRTCRoomConnectedOptionsInternal : ISettable<IsRTCRoomConnectedOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId;

	private IntPtr m_LocalUserId;

	public void Set(ref IsRTCRoomConnectedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.LobbyId, ref m_LobbyId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyId);
		Helper.Dispose(ref m_LocalUserId);
	}
}
