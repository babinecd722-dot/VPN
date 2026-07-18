using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyLastRedeemedEntitlementByIndexOptionsInternal : ISettable<CopyLastRedeemedEntitlementByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_RedeemedEntitlementIndex;

	public void Set(ref CopyLastRedeemedEntitlementByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_RedeemedEntitlementIndex = other.RedeemedEntitlementIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
