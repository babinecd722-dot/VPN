using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct WindowsSteamOptionsInternal : ISettable<WindowsSteamOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Type;

	private IntegratedPlatformManagementFlags m_Flags;

	private IntPtr m_InitOptions;

	public void Set(ref WindowsSteamOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Type, ref m_Type);
		m_Flags = other.Flags;
		Helper.Set<WindowsSteamOptionsInitOptions, WindowsSteamOptionsInitOptionsInternal>(other.InitOptions, ref m_InitOptions);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Type);
		Helper.Dispose(ref m_InitOptions);
	}
}
