using System;

namespace Epic.OnlineServices.Ecom;

internal struct RedeemEntitlementsOptionsInternal : ISettable<RedeemEntitlementsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_EntitlementIdCount;

	private IntPtr m_EntitlementIds;

	public void Set(ref RedeemEntitlementsOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.EntitlementIds, ref m_EntitlementIds, out m_EntitlementIdCount, isArrayItemAllocated: true);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_EntitlementIds);
	}
}
