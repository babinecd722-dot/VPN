using System;

namespace Epic.OnlineServices.Auth;

internal struct CopyIdTokenOptionsInternal : ISettable<CopyIdTokenOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AccountId;

	public void Set(ref CopyIdTokenOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.AccountId, ref m_AccountId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AccountId);
	}
}
