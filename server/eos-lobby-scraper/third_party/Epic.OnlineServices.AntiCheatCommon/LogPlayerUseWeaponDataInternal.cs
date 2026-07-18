using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerUseWeaponDataInternal : ISettable<LogPlayerUseWeaponData>, IDisposable
{
	private IntPtr m_PlayerHandle;

	private IntPtr m_PlayerPosition;

	private IntPtr m_PlayerViewRotation;

	private int m_IsPlayerViewZoomed;

	private int m_IsMeleeAttack;

	private IntPtr m_WeaponName;

	public void Set(ref LogPlayerUseWeaponData other)
	{
		Dispose();
		m_PlayerHandle = other.PlayerHandle;
		Helper.Set<Vec3f, Vec3fInternal>(other.PlayerPosition, ref m_PlayerPosition);
		Helper.Set<Quat, QuatInternal>(other.PlayerViewRotation, ref m_PlayerViewRotation);
		Helper.Set(other.IsPlayerViewZoomed, ref m_IsPlayerViewZoomed);
		Helper.Set(other.IsMeleeAttack, ref m_IsMeleeAttack);
		Helper.Set(other.WeaponName, ref m_WeaponName);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlayerPosition);
		Helper.Dispose(ref m_PlayerViewRotation);
		Helper.Dispose(ref m_WeaponName);
	}
}
