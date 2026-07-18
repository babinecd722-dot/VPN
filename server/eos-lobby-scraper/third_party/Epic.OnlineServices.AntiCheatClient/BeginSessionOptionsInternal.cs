using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct BeginSessionOptionsInternal : ISettable<BeginSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private AntiCheatClientMode m_Mode;

	public void Set(ref BeginSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		m_Mode = other.Mode;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
