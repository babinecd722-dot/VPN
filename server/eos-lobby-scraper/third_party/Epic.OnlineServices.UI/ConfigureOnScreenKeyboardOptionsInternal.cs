using System;

namespace Epic.OnlineServices.UI;

internal struct ConfigureOnScreenKeyboardOptionsInternal : ISettable<ConfigureOnScreenKeyboardOptions>, IDisposable
{
	private int m_ApiVersion;

	private OnScreenKeyboardBehavior m_Behavior;

	private int m_IsDeviceChecksEnabled;

	public void Set(ref ConfigureOnScreenKeyboardOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_Behavior = other.Behavior;
		Helper.Set(other.IsDeviceChecksEnabled, ref m_IsDeviceChecksEnabled);
	}

	public void Dispose()
	{
	}
}
