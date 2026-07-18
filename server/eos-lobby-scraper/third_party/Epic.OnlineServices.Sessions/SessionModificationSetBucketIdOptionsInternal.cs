using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationSetBucketIdOptionsInternal : ISettable<SessionModificationSetBucketIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_BucketId;

	public void Set(ref SessionModificationSetBucketIdOptions other)
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
