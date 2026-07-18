using System;

namespace Epic.OnlineServices.RTC;

internal struct SetRoomSettingOptionsInternal : ISettable<SetRoomSettingOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_SettingName;

	private IntPtr m_SettingValue;

	public void Set(ref SetRoomSettingOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.RoomName, ref m_RoomName);
		Helper.Set(other.SettingName, ref m_SettingName);
		Helper.Set(other.SettingValue, ref m_SettingValue);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_RoomName);
		Helper.Dispose(ref m_SettingName);
		Helper.Dispose(ref m_SettingValue);
	}
}
