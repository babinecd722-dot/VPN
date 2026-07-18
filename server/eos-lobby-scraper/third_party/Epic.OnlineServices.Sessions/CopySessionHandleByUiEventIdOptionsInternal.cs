using System;

namespace Epic.OnlineServices.Sessions;

internal struct CopySessionHandleByUiEventIdOptionsInternal : ISettable<CopySessionHandleByUiEventIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private ulong m_UiEventId;

	public void Set(ref CopySessionHandleByUiEventIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_UiEventId = other.UiEventId;
	}

	public void Dispose()
	{
	}
}
