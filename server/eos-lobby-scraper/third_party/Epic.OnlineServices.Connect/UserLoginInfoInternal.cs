using System;

namespace Epic.OnlineServices.Connect;

internal struct UserLoginInfoInternal : ISettable<UserLoginInfo>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_DisplayName;

	private IntPtr m_NsaIdToken;

	public void Set(ref UserLoginInfo other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.DisplayName, ref m_DisplayName);
		Helper.Set(other.NsaIdToken, ref m_NsaIdToken);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_DisplayName);
		Helper.Dispose(ref m_NsaIdToken);
	}
}
