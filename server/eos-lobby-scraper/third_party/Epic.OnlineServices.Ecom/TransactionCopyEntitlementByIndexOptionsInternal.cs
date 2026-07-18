using System;

namespace Epic.OnlineServices.Ecom;

internal struct TransactionCopyEntitlementByIndexOptionsInternal : ISettable<TransactionCopyEntitlementByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_EntitlementIndex;

	public void Set(ref TransactionCopyEntitlementByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_EntitlementIndex = other.EntitlementIndex;
	}

	public void Dispose()
	{
	}
}
