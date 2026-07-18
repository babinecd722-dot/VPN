using System;

namespace Epic.OnlineServices.UI;

internal struct GetToggleFriendsButtonOptionsInternal : ISettable<GetToggleFriendsButtonOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetToggleFriendsButtonOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
