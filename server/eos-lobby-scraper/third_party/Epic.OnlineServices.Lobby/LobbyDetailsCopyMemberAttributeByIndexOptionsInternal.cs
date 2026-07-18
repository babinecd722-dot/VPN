using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsCopyMemberAttributeByIndexOptionsInternal : ISettable<LobbyDetailsCopyMemberAttributeByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private uint m_AttrIndex;

	public void Set(ref LobbyDetailsCopyMemberAttributeByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_AttrIndex = other.AttrIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
	}
}
