using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyOfferByIndexOptionsInternal : ISettable<CopyOfferByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_OfferIndex;

	public void Set(ref CopyOfferByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_OfferIndex = other.OfferIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
