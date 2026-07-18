using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyModificationAddAttributeOptionsInternal : ISettable<LobbyModificationAddAttributeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Attribute;

	private LobbyAttributeVisibility m_Visibility;

	public void Set(ref LobbyModificationAddAttributeOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set<AttributeData, AttributeDataInternal>(other.Attribute, ref m_Attribute);
		m_Visibility = other.Visibility;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Attribute);
	}
}
