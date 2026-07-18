using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct GetAudioOutputDeviceByIndexOptionsInternal : ISettable<GetAudioOutputDeviceByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_DeviceInfoIndex;

	public void Set(ref GetAudioOutputDeviceByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_DeviceInfoIndex = other.DeviceInfoIndex;
	}

	public void Dispose()
	{
	}
}
