using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct WindowsSteamOptionsInitOptionsInternal : ISettable<WindowsSteamOptionsInitOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_OverrideLibraryPath;

	private uint m_SteamMajorVersion;

	private uint m_SteamMinorVersion;

	private IntPtr m_SteamApiInterfaceVersionsArray;

	private uint m_SteamApiInterfaceVersionsArrayBytes;

	public void Set(ref WindowsSteamOptionsInitOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set(other.OverrideLibraryPath, ref m_OverrideLibraryPath);
		m_SteamMajorVersion = other.SteamMajorVersion;
		m_SteamMinorVersion = other.SteamMinorVersion;
		Helper.Set(other.SteamApiInterfaceVersionsArray, ref m_SteamApiInterfaceVersionsArray);
		m_SteamApiInterfaceVersionsArrayBytes = other.SteamApiInterfaceVersionsArrayBytes;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_OverrideLibraryPath);
		Helper.Dispose(ref m_SteamApiInterfaceVersionsArray);
	}
}
