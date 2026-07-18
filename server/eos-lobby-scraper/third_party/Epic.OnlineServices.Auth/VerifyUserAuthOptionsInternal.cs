using System;

namespace Epic.OnlineServices.Auth;

internal struct VerifyUserAuthOptionsInternal : ISettable<VerifyUserAuthOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AuthToken;

	public void Set(ref VerifyUserAuthOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<Token, TokenInternal>(other.AuthToken, ref m_AuthToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AuthToken);
	}
}
