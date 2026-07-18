using System;

namespace Epic.OnlineServices.AntiCheatServer;

internal struct BeginSessionOptionsInternal : ISettable<BeginSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_RegisterTimeoutSeconds;

	private IntPtr m_ServerName;

	private int m_EnableGameplayData;

	private IntPtr m_LocalUserId;

	public void Set(ref BeginSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		m_RegisterTimeoutSeconds = other.RegisterTimeoutSeconds;
		Helper.Set(other.ServerName, ref m_ServerName);
		Helper.Set(other.EnableGameplayData, ref m_EnableGameplayData);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ServerName);
		Helper.Dispose(ref m_LocalUserId);
	}
}
