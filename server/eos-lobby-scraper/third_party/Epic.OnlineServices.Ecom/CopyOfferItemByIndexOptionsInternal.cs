using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyOfferItemByIndexOptionsInternal : ISettable<CopyOfferItemByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_OfferId;

	private uint m_ItemIndex;

	public void Set(ref CopyOfferItemByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.OfferId, ref m_OfferId);
		m_ItemIndex = other.ItemIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_OfferId);
	}
}
