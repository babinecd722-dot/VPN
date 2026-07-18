using System;

namespace Epic.OnlineServices.Ecom;

internal struct CopyTransactionByIdOptionsInternal : ISettable<CopyTransactionByIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_TransactionId;

	public void Set(ref CopyTransactionByIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.TransactionId, ref m_TransactionId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TransactionId);
	}
}
