using System;

namespace Epic.OnlineServices.Ecom;

internal struct CheckoutEntryInternal : ISettable<CheckoutEntry>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_OfferId;

	public void Set(ref CheckoutEntry other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.OfferId, ref m_OfferId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_OfferId);
	}
}
