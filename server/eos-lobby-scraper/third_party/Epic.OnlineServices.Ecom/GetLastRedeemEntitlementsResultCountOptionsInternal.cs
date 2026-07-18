using System;

namespace Epic.OnlineServices.Ecom;

internal struct GetLastRedeemEntitlementsResultCountOptionsInternal : ISettable<GetLastRedeemEntitlementsResultCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private RedeemEntitlementsResultListType m_ResultType;

	public void Set(ref GetLastRedeemEntitlementsResultCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_ResultType = other.ResultType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
