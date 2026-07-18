using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct GetAudioInputDeviceByIndexOptionsInternal : ISettable<GetAudioInputDeviceByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_DeviceInfoIndex;

	public void Set(ref GetAudioInputDeviceByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_DeviceInfoIndex = other.DeviceInfoIndex;
	}

	public void Dispose()
	{
	}
}
