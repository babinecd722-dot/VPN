using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AudioOutputDeviceInfoInternal : IGettable<AudioOutputDeviceInfo>
{
	private int m_ApiVersion;

	private int m_DefaultDevice;

	private IntPtr m_DeviceId;

	private IntPtr m_DeviceName;

	public void Get(out AudioOutputDeviceInfo other)
	{
		other = default(AudioOutputDeviceInfo);
		Helper.Get(m_DefaultDevice, out bool to);
		other.DefaultDevice = to;
		Helper.Get(m_DeviceId, out Utf8String to2);
		other.DeviceId = to2;
		Helper.Get(m_DeviceName, out Utf8String to3);
		other.DeviceName = to3;
	}
}
