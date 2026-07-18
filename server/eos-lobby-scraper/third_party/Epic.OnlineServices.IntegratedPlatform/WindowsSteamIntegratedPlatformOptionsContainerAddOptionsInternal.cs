using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct WindowsSteamIntegratedPlatformOptionsContainerAddOptionsInternal : ISettable<WindowsSteamIntegratedPlatformOptionsContainerAddOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Options;

	public void Set(ref WindowsSteamIntegratedPlatformOptionsContainerAddOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<WindowsSteamOptions, WindowsSteamOptionsInternal>(other.Options, ref m_Options);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Options);
	}
}
