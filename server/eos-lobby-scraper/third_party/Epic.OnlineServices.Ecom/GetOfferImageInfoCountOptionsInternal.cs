using System;

namespace Epic.OnlineServices.Ecom;

internal struct GetOfferImageInfoCountOptionsInternal : ISettable<GetOfferImageInfoCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_OfferId;

	public void Set(ref GetOfferImageInfoCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.OfferId, ref m_OfferId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_OfferId);
	}
}
