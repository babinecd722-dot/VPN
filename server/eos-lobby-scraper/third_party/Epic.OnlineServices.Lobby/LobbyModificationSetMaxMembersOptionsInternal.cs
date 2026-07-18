using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyModificationSetMaxMembersOptionsInternal : ISettable<LobbyModificationSetMaxMembersOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_MaxMembers;

	public void Set(ref LobbyModificationSetMaxMembersOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_MaxMembers = other.MaxMembers;
	}

	public void Dispose()
	{
	}
}
