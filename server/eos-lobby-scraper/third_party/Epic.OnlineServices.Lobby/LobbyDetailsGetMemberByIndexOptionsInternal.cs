using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsGetMemberByIndexOptionsInternal : ISettable<LobbyDetailsGetMemberByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_MemberIndex;

	public void Set(ref LobbyDetailsGetMemberByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_MemberIndex = other.MemberIndex;
	}

	public void Dispose()
	{
	}
}
