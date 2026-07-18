using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct InputDeviceInformationInternal : IGettable<InputDeviceInformation>
{
	private int m_ApiVersion;

	private int m_DefaultDevice;

	private IntPtr m_DeviceId;

	private IntPtr m_DeviceName;

	public void Get(out InputDeviceInformation other)
	{
		other = default(InputDeviceInformation);
		Helper.Get(m_DefaultDevice, out bool to);
		other.DefaultDevice = to;
		Helper.Get(m_DeviceId, out Utf8String to2);
		other.DeviceId = to2;
		Helper.Get(m_DeviceName, out Utf8String to3);
		other.DeviceName = to3;
	}
}
