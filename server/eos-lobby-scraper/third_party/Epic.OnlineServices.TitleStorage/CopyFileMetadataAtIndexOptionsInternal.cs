using System;

namespace Epic.OnlineServices.TitleStorage;

internal struct CopyFileMetadataAtIndexOptionsInternal : ISettable<CopyFileMetadataAtIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_Index;

	public void Set(ref CopyFileMetadataAtIndexOptions other)
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
