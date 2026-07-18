using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct SetClientDetailsOptionsInternal : ISettable<SetClientDetailsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_ClientHandle;

	private AntiCheatCommonClientFlags m_ClientFlags;

	private AntiCheatCommonClientInput m_ClientInputMethod;

	public void Set(ref SetClientDetailsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_ClientHandle = other.ClientHandle;
		m_ClientFlags = other.ClientFlags;
		m_ClientInputMethod = other.ClientInputMethod;
	}

	public void Dispose()
	{
	}
}
