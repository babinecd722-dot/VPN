using System;

namespace Epic.OnlineServices.Ecom;

internal struct CheckoutOptionsInternal : ISettable<CheckoutOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_OverrideCatalogNamespace;

	private uint m_EntryCount;

	private IntPtr m_Entries;

	private CheckoutOrientation m_PreferredOrientation;

	public void Set(ref CheckoutOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.OverrideCatalogNamespace, ref m_OverrideCatalogNamespace);
		Helper.Set<CheckoutEntry, CheckoutEntryInternal>(other.Entries, ref m_Entries, out m_EntryCount, isArrayItemAllocated: false);
		m_PreferredOrientation = other.PreferredOrientation;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_OverrideCatalogNamespace);
		Helper.Dispose(ref m_Entries);
	}
}
