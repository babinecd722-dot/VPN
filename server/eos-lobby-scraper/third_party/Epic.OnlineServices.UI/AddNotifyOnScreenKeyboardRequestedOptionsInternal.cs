using System;

namespace Epic.OnlineServices.UI;

internal struct AddNotifyOnScreenKeyboardRequestedOptionsInternal : ISettable<AddNotifyOnScreenKeyboardRequestedOptions>, IDisposable
{
	private int m_ApiVersion;

	public void Set(ref AddNotifyOnScreenKeyboardRequestedOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
	}

	public void Dispose()
	{
	}
}
