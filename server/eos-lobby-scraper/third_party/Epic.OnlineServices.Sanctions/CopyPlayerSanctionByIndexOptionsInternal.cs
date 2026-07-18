using System;

namespace Epic.OnlineServices.Sanctions;

internal struct CopyPlayerSanctionByIndexOptionsInternal : ISettable<CopyPlayerSanctionByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private uint m_SanctionIndex;

	public void Set(ref CopyPlayerSanctionByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		m_SanctionIndex = other.SanctionIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
	}
}
