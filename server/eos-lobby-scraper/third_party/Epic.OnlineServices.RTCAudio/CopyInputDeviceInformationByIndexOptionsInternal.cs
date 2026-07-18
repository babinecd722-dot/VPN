using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct CopyInputDeviceInformationByIndexOptionsInternal : ISettable<CopyInputDeviceInformationByIndexOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_DeviceIndex;

	public void Set(ref CopyInputDeviceInformationByIndexOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_DeviceIndex = other.DeviceIndex;
	}

	public void Dispose()
	{
	}
}
