using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyEntitlementByIndexOptionsInternal : ISettable<CopyEntitlementByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_EntitlementIndex;

	public void Set(ref CopyEntitlementByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_EntitlementIndex = other.EntitlementIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
