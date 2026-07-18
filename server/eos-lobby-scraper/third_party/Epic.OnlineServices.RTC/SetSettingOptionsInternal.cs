using System;

namespace Epic.OnlineServices.RTC;

internal struct SetSettingOptionsInternal : ISettable<SetSettingOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SettingName;

	private IntPtr m_SettingValue;

	public void Set(ref SetSettingOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.SettingName, ref m_SettingName);
		Helper.Set(other.SettingValue, ref m_SettingValue);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SettingName);
		Helper.Dispose(ref m_SettingValue);
	}
}
