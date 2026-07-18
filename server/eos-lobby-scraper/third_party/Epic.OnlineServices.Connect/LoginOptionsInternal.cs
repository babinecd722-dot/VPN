using System;

namespace Epic.OnlineServices.Connect;

internal struct LoginOptionsInternal : ISettable<LoginOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Credentials;

	private IntPtr m_UserLoginInfo;

	public void Set(ref LoginOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set<Credentials, CredentialsInternal>(other.Credentials, ref m_Credentials);
		Helper.Set<UserLoginInfo, UserLoginInfoInternal>(other.UserLoginInfo, ref m_UserLoginInfo);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Credentials);
		Helper.Dispose(ref m_UserLoginInfo);
	}
}
