using System;

namespace Epic.OnlineServices.Lobby;

internal struct AttributeInternal : IGettable<Attribute>
{
	private int m_ApiVersion;

	private IntPtr m_Data;

	private LobbyAttributeVisibility m_Visibility;

	public void Get(out Attribute other)
	{
		other = default(Attribute);
		Helper.Get<AttributeDataInternal, AttributeData>(m_Data, out AttributeData? to);
		other.Data = to;
		other.Visibility = m_Visibility;
	}
}
