using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyItemImageInfoByIndexOptionsInternal : ISettable<CopyItemImageInfoByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ItemId;

	private uint m_ImageInfoIndex;

	public void Set(ref CopyItemImageInfoByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.ItemId, ref m_ItemId);
		m_ImageInfoIndex = other.ImageInfoIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ItemId);
	}
}
