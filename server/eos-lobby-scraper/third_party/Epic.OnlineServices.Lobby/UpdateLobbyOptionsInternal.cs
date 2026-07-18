using System;

namespace Epic.OnlineServices.Lobby;

internal struct UpdateLobbyOptionsInternal : ISettable<UpdateLobbyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyModificationHandle;

	public void Set(ref UpdateLobbyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LobbyModificationHandle, ref m_LobbyModificationHandle);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyModificationHandle);
	}
}
