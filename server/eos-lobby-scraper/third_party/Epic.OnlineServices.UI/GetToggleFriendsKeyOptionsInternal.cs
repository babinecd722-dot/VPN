using System;

namespace Epic.OnlineServices.UI;

internal struct GetToggleFriendsKeyOptionsInternal : ISettable<GetToggleFriendsKeyOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref GetToggleFriendsKeyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
