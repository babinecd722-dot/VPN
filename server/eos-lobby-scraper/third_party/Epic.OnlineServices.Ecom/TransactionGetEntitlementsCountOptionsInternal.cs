using System;

namespace Epic.OnlineServices.Ecom;

internal struct TransactionGetEntitlementsCountOptionsInternal : ISettable<TransactionGetEntitlementsCountOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref TransactionGetEntitlementsCountOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
