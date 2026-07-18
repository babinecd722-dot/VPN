using System;

namespace Epic.OnlineServices.Lobby;

internal struct AddNotifyRTCRoomConnectionChangedOptionsInternal : ISettable<AddNotifyRTCRoomConnectionChangedOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId_DEPRECATED;

	private IntPtr m_LocalUserId_DEPRECATED;

	public void Set(ref AddNotifyRTCRoomConnectionChangedOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.LobbyId_DEPRECATED, ref m_LobbyId_DEPRECATED);
		Helper.Set((Handle)other.LocalUserId_DEPRECATED, ref m_LocalUserId_DEPRECATED);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyId_DEPRECATED);
		Helper.Dispose(ref m_LocalUserId_DEPRECATED);
	}
}
