using System;

namespace Epic.OnlineServices.Auth;

internal struct IdTokenInternal : IGettable<IdToken>, ISettable<IdToken>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_AccountId;

	private IntPtr m_JsonWebToken;

	public void Get(out IdToken other)
	{
		other = default(IdToken);
		Helper.Get(m_AccountId, out EpicAccountId to);
		other.AccountId = to;
		Helper.Get(m_JsonWebToken, out Utf8String to2);
		other.JsonWebToken = to2;
	}

	public void Set(ref IdToken other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.AccountId, ref m_AccountId);
		Helper.Set(other.JsonWebToken, ref m_JsonWebToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AccountId);
		Helper.Dispose(ref m_JsonWebToken);
	}
}
