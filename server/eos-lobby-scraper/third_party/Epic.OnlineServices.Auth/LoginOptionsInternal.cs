using System;

namespace Epic.OnlineServices.Auth;

internal struct LoginOptionsInternal : ISettable<LoginOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Credentials;

	private AuthScopeFlags m_ScopeFlags;

	private LoginFlags m_LoginFlags;

	public void Set(ref LoginOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set<Credentials, CredentialsInternal>(other.Credentials, ref m_Credentials);
		m_ScopeFlags = other.ScopeFlags;
		m_LoginFlags = other.LoginFlags;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Credentials);
	}
}
