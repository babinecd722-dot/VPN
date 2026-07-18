using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerUseWeaponOptionsInternal : ISettable<LogPlayerUseWeaponOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_UseWeaponData;

	public void Set(ref LogPlayerUseWeaponOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set<LogPlayerUseWeaponData, LogPlayerUseWeaponDataInternal>(other.UseWeaponData, ref m_UseWeaponData);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_UseWeaponData);
	}
}
