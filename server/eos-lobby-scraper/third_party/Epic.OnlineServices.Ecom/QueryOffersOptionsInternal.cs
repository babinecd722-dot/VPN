using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryOffersOptionsInternal : ISettable<QueryOffersOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_OverrideCatalogNamespace;

	public void Set(ref QueryOffersOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.OverrideCatalogNamespace, ref m_OverrideCatalogNamespace);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_OverrideCatalogNamespace);
	}
}
