using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyModificationSetBucketIdOptionsInternal : ISettable<LobbyModificationSetBucketIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_BucketId;

	public void Set(ref LobbyModificationSetBucketIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.BucketId, ref m_BucketId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_BucketId);
	}
}
