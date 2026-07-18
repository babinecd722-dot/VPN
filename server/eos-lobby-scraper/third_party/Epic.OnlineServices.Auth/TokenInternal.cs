using System;

namespace Epic.OnlineServices.Auth;

internal struct TokenInternal : IGettable<Token>, ISettable<Token>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_App;

	private IntPtr m_ClientId;

	private IntPtr m_AccountId;

	private IntPtr m_AccessToken;

	private double m_ExpiresIn;

	private IntPtr m_ExpiresAt;

	private AuthTokenType m_AuthType;

	private IntPtr m_RefreshToken;

	private double m_RefreshExpiresIn;

	private IntPtr m_RefreshExpiresAt;

	public void Get(out Token other)
	{
		other = default(Token);
		Helper.Get(m_App, out Utf8String to);
		other.App = to;
		Helper.Get(m_ClientId, out Utf8String to2);
		other.ClientId = to2;
		Helper.Get(m_AccountId, out EpicAccountId to3);
		other.AccountId = to3;
		Helper.Get(m_AccessToken, out Utf8String to4);
		other.AccessToken = to4;
		other.ExpiresIn = m_ExpiresIn;
		Helper.Get(m_ExpiresAt, out Utf8String to5);
		other.ExpiresAt = to5;
		other.AuthType = m_AuthType;
		Helper.Get(m_RefreshToken, out Utf8String to6);
		other.RefreshToken = to6;
		other.RefreshExpiresIn = m_RefreshExpiresIn;
		Helper.Get(m_RefreshExpiresAt, out Utf8String to7);
		other.RefreshExpiresAt = to7;
	}

	public void Set(ref Token other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.App, ref m_App);
		Helper.Set(other.ClientId, ref m_ClientId);
		Helper.Set((Handle)other.AccountId, ref m_AccountId);
		Helper.Set(other.AccessToken, ref m_AccessToken);
		m_ExpiresIn = other.ExpiresIn;
		Helper.Set(other.ExpiresAt, ref m_ExpiresAt);
		m_AuthType = other.AuthType;
		Helper.Set(other.RefreshToken, ref m_RefreshToken);
		m_RefreshExpiresIn = other.RefreshExpiresIn;
		Helper.Set(other.RefreshExpiresAt, ref m_RefreshExpiresAt);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_App);
		Helper.Dispose(ref m_ClientId);
		Helper.Dispose(ref m_AccountId);
		Helper.Dispose(ref m_AccessToken);
		Helper.Dispose(ref m_ExpiresAt);
		Helper.Dispose(ref m_RefreshToken);
		Helper.Dispose(ref m_RefreshExpiresAt);
	}
}
