using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryEntitlementTokenOptionsInternal : ISettable<QueryEntitlementTokenOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_EntitlementNames;

	private uint m_EntitlementNameCount;

	public void Set(ref QueryEntitlementTokenOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.EntitlementNames, ref m_EntitlementNames, out m_EntitlementNameCount, isArrayItemAllocated: true);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EntitlementNames);
	}
}
