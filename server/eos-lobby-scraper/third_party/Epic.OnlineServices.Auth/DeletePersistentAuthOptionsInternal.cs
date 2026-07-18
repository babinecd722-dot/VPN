using System;

namespace Epic.OnlineServices.Auth;

internal struct DeletePersistentAuthOptionsInternal : ISettable<DeletePersistentAuthOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_RefreshToken;

	public void Set(ref DeletePersistentAuthOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.RefreshToken, ref m_RefreshToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_RefreshToken);
	}
}
