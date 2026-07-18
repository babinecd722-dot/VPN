using System;

namespace Epic.OnlineServices.Platform;

internal struct ClientCredentialsInternal : ISettable<ClientCredentials>, IDisposable
{
	private IntPtr m_ClientId;

	private IntPtr m_ClientSecret;

	public void Set(ref ClientCredentials other)
	{
		Dispose();
		Helper.Set(other.ClientId, ref m_ClientId);
		Helper.Set(other.ClientSecret, ref m_ClientSecret);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ClientId);
		Helper.Dispose(ref m_ClientSecret);
	}
}
