using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AudioInputDeviceInfoInternal : IGettable<AudioInputDeviceInfo>
{
	private int m_ApiVersion;

	private int m_DefaultDevice;

	private IntPtr m_DeviceId;

	private IntPtr m_DeviceName;

	public void Get(out AudioInputDeviceInfo other)
	{
		other = default(AudioInputDeviceInfo);
		Helper.Get(m_DefaultDevice, out bool to);
		other.DefaultDevice = to;
		Helper.Get(m_DeviceId, out Utf8String to2);
		other.DeviceId = to2;
		Helper.Get(m_DeviceName, out Utf8String to3);
		other.DeviceName = to3;
	}
}
