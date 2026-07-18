using System;

namespace Epic.OnlineServices.Auth;

internal struct CredentialsInternal : ISettable<Credentials>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Id;

	private IntPtr m_Token;

	private LoginCredentialType m_Type;

	private IntPtr m_SystemAuthCredentialsOptions;

	private ExternalCredentialType m_ExternalType;

	public void Set(ref Credentials other)
	{
		Dispose();
		m_ApiVersion = 4;
		Helper.Set(other.Id, ref m_Id);
		Helper.Set(other.Token, ref m_Token);
		m_Type = other.Type;
		m_SystemAuthCredentialsOptions = other.SystemAuthCredentialsOptions;
		m_ExternalType = other.ExternalType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Id);
		Helper.Dispose(ref m_Token);
		Helper.Dispose(ref m_SystemAuthCredentialsOptions);
	}
}
