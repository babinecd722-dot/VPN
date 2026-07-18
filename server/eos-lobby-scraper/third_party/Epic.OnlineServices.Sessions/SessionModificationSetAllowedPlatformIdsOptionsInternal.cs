using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationSetAllowedPlatformIdsOptionsInternal : ISettable<SessionModificationSetAllowedPlatformIdsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AllowedPlatformIds;

	private uint m_AllowedPlatformIdsCount;

	public void Set(ref SessionModificationSetAllowedPlatformIdsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.AllowedPlatformIds, ref m_AllowedPlatformIds, out m_AllowedPlatformIdsCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AllowedPlatformIds);
	}
}
