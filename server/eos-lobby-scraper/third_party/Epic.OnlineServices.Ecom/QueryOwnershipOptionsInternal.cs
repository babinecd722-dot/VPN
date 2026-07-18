using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryOwnershipOptionsInternal : ISettable<QueryOwnershipOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_CatalogItemIds;

	private uint m_CatalogItemIdCount;

	private IntPtr m_CatalogNamespace;

	public void Set(ref QueryOwnershipOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.CatalogItemIds, ref m_CatalogItemIds, out m_CatalogItemIdCount, isArrayItemAllocated: true);
		Helper.Set(other.CatalogNamespace, ref m_CatalogNamespace);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_CatalogItemIds);
		Helper.Dispose(ref m_CatalogNamespace);
	}
}
