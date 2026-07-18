using System;

namespace Epic.OnlineServices.Friends;

internal struct AddNotifyFriendsUpdateOptionsInternal : ISettable<AddNotifyFriendsUpdateOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyFriendsUpdateOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
