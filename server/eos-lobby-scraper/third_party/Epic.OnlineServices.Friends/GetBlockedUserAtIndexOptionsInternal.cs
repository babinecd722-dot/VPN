using System;

namespace Epic.OnlineServices.Friends;

internal struct GetBlockedUserAtIndexOptionsInternal : ISettable<GetBlockedUserAtIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private int m_Index;

	public void Set(ref GetBlockedUserAtIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_Index = other.Index;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
