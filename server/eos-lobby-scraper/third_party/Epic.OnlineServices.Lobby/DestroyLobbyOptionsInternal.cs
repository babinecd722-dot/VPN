using System;

namespace Epic.OnlineServices.Lobby;

internal struct DestroyLobbyOptionsInternal : ISettable<DestroyLobbyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_LobbyId;

	public void Set(ref DestroyLobbyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.LobbyId, ref m_LobbyId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_LobbyId);
	}
}
