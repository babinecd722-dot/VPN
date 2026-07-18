using System;

namespace Epic.OnlineServices.Stats;

internal struct CopyStatByIndexOptionsInternal : ISettable<CopyStatByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private uint m_StatIndex;

	public void Set(ref CopyStatByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_StatIndex = other.StatIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
	}
}
