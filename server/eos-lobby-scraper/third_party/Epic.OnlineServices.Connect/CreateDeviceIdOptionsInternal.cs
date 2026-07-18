using System;

namespace Epic.OnlineServices.Connect;

internal struct CreateDeviceIdOptionsInternal : ISettable<CreateDeviceIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_DeviceModel;

	public void Set(ref CreateDeviceIdOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.DeviceModel, ref m_DeviceModel);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_DeviceModel);
	}
}
