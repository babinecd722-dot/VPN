using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyModificationAddAttributeOptionsInternal : ISettable<LobbyModificationAddAttributeOptions>, IDisposable
{
	public LobbyModificationAddAttributeOptionsInternal()
	{
	}

	private int m_ApiVersion = default;

	internal int ApiVersion => m_ApiVersion;

	private IntPtr m_Attribute = default;

	private LobbyAttributeVisibility m_Visibility = default;

	public void Set(ref LobbyModificationAddAttributeOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<AttributeData, AttributeDataInternal>(other.Attribute, ref m_Attribute);
		m_Visibility = other.Visibility;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Attribute);
	}
}
