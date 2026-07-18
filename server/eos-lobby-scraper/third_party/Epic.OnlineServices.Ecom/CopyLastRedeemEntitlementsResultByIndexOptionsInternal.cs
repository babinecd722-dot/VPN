using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyLastRedeemEntitlementsResultByIndexOptionsInternal : ISettable<CopyLastRedeemEntitlementsResultByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_EntitlementIndex;

	private RedeemEntitlementsResultListType m_ResultType;

	public void Set(ref CopyLastRedeemEntitlementsResultByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_EntitlementIndex = other.EntitlementIndex;
		m_ResultType = other.ResultType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
