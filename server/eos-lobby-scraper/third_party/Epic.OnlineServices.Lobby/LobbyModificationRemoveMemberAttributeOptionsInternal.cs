using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyModificationRemoveMemberAttributeOptionsInternal : ISettable<LobbyModificationRemoveMemberAttributeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	public void Set(ref LobbyModificationRemoveMemberAttributeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Key, ref m_Key);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Key);
	}
}
