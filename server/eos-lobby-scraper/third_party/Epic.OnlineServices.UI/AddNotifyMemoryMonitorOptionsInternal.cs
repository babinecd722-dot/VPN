using System;

namespace Epic.OnlineServices.UI;

internal struct AddNotifyMemoryMonitorOptionsInternal : ISettable<AddNotifyMemoryMonitorOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyMemoryMonitorOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
