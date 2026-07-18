using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsCopyAttributeByKeyOptionsInternal : ISettable<LobbyDetailsCopyAttributeByKeyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AttrKey;

	public void Set(ref LobbyDetailsCopyAttributeByKeyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.AttrKey, ref m_AttrKey);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AttrKey);
	}
}
