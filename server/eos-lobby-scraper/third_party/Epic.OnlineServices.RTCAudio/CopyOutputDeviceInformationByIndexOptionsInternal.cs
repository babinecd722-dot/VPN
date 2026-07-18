using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct CopyOutputDeviceInformationByIndexOptionsInternal : ISettable<CopyOutputDeviceInformationByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_DeviceIndex;

	public void Set(ref CopyOutputDeviceInformationByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_DeviceIndex = other.DeviceIndex;
	}

	public void Dispose()
	{
	}
}
