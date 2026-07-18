using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerReviveOptionsInternal : ISettable<LogPlayerReviveOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_RevivedPlayerHandle;

	private IntPtr m_ReviverPlayerHandle;

	public void Set(ref LogPlayerReviveOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_RevivedPlayerHandle = other.RevivedPlayerHandle;
		m_ReviverPlayerHandle = other.ReviverPlayerHandle;
	}

	public void Dispose()
	{
	}
}
