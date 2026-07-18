using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyTransactionByIndexOptionsInternal : ISettable<CopyTransactionByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private uint m_TransactionIndex;

	public void Set(ref CopyTransactionByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_TransactionIndex = other.TransactionIndex;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
