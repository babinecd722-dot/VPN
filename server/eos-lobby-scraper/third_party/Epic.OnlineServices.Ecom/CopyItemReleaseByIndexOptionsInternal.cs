using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyItemReleaseByIndexOptionsInternal : ISettable<CopyItemReleaseByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ItemId;

	private uint m_ReleaseIndex;

	public void Set(ref CopyItemReleaseByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.ItemId, ref m_ItemId);
		m_ReleaseIndex = other.ReleaseIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ItemId);
	}
}
