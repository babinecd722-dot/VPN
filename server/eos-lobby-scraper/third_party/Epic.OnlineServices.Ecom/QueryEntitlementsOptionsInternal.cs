using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryEntitlementsOptionsInternal : ISettable<QueryEntitlementsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_EntitlementNames;

	private uint m_EntitlementNameCount;

	private int m_IncludeRedeemed;

	private IntPtr m_OverrideCatalogNamespace;

	public void Set(ref QueryEntitlementsOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.EntitlementNames, ref m_EntitlementNames, out m_EntitlementNameCount, isArrayItemAllocated: true);
		Helper.Set(other.IncludeRedeemed, ref m_IncludeRedeemed);
		Helper.Set(other.OverrideCatalogNamespace, ref m_OverrideCatalogNamespace);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EntitlementNames);
		Helper.Dispose(ref m_OverrideCatalogNamespace);
	}
}
