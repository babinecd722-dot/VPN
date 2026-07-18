using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyEntitlementByNameAndIndexOptionsInternal : ISettable<CopyEntitlementByNameAndIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_EntitlementName;

	private uint m_Index;

	public void Set(ref CopyEntitlementByNameAndIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.EntitlementName, ref m_EntitlementName);
		m_Index = other.Index;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EntitlementName);
	}
}
