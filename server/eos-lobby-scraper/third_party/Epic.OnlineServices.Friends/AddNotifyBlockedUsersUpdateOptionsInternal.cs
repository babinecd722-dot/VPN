using System;

namespace Epic.OnlineServices.Friends;

internal struct AddNotifyBlockedUsersUpdateOptionsInternal : ISettable<AddNotifyBlockedUsersUpdateOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyBlockedUsersUpdateOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
