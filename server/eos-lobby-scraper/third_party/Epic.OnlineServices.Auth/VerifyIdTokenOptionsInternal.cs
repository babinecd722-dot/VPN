using System;

namespace Epic.OnlineServices.Auth;

internal struct VerifyIdTokenOptionsInternal : ISettable<VerifyIdTokenOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_IdToken;

	public void Set(ref VerifyIdTokenOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<IdToken, IdTokenInternal>(other.IdToken, ref m_IdToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_IdToken);
	}
}
