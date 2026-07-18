using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct SetInputDeviceSettingsOptionsInternal : ISettable<SetInputDeviceSettingsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RealDeviceId;

	private int m_PlatformAEC;

	public void Set(ref SetInputDeviceSettingsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RealDeviceId, ref m_RealDeviceId);
		Helper.Set(other.PlatformAEC, ref m_PlatformAEC);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RealDeviceId);
	}
}
