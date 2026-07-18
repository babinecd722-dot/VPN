using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct SetOutputDeviceSettingsOptionsInternal : ISettable<SetOutputDeviceSettingsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RealDeviceId;

	public void Set(ref SetOutputDeviceSettingsOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RealDeviceId, ref m_RealDeviceId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RealDeviceId);
	}
}
