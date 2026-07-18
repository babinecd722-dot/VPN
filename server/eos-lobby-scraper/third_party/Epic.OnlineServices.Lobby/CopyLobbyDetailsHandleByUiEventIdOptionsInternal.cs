using System;

namespace Epic.OnlineServices.Lobby;

internal struct CopyLobbyDetailsHandleByUiEventIdOptionsInternal : ISettable<CopyLobbyDetailsHandleByUiEventIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private ulong m_UiEventId;

	public void Set(ref CopyLobbyDetailsHandleByUiEventIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_UiEventId = other.UiEventId;
	}

	public void Dispose()
	{
	}
}
