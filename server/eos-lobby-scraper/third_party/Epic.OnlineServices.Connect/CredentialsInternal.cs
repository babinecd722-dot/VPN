using System;

namespace Epic.OnlineServices.Connect;

internal struct CredentialsInternal : ISettable<Credentials>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Token;

	private ExternalCredentialType m_Type;

	public void Set(ref Credentials other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Token, ref m_Token);
		m_Type = other.Type;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Token);
	}
}
