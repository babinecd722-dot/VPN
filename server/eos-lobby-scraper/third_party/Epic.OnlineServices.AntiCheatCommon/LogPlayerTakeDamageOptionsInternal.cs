using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerTakeDamageOptionsInternal : ISettable<LogPlayerTakeDamageOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_VictimPlayerHandle;

	private IntPtr m_VictimPlayerPosition;

	private IntPtr m_VictimPlayerViewRotation;

	private IntPtr m_AttackerPlayerHandle;

	private IntPtr m_AttackerPlayerPosition;

	private IntPtr m_AttackerPlayerViewRotation;

	private int m_IsHitscanAttack;

	private int m_HasLineOfSight;

	private int m_IsCriticalHit;

	private uint m_HitBoneId_DEPRECATED;

	private float m_DamageTaken;

	private float m_HealthRemaining;

	private AntiCheatCommonPlayerTakeDamageSource m_DamageSource;

	private AntiCheatCommonPlayerTakeDamageType m_DamageType;

	private AntiCheatCommonPlayerTakeDamageResult m_DamageResult;

	private IntPtr m_PlayerUseWeaponData;

	private uint m_TimeSincePlayerUseWeaponMs;

	private IntPtr m_DamagePosition;

	private IntPtr m_AttackerPlayerViewPosition;

	public void Set(ref LogPlayerTakeDamageOptions other)
	{
		Dispose();
		m_ApiVersion = 4;
		m_VictimPlayerHandle = other.VictimPlayerHandle;
		Helper.Set<Vec3f, Vec3fInternal>(other.VictimPlayerPosition, ref m_VictimPlayerPosition);
		Helper.Set<Quat, QuatInternal>(other.VictimPlayerViewRotation, ref m_VictimPlayerViewRotation);
		m_AttackerPlayerHandle = other.AttackerPlayerHandle;
		Helper.Set<Vec3f, Vec3fInternal>(other.AttackerPlayerPosition, ref m_AttackerPlayerPosition);
		Helper.Set<Quat, QuatInternal>(other.AttackerPlayerViewRotation, ref m_AttackerPlayerViewRotation);
		Helper.Set(other.IsHitscanAttack, ref m_IsHitscanAttack);
		Helper.Set(other.HasLineOfSight, ref m_HasLineOfSight);
		Helper.Set(other.IsCriticalHit, ref m_IsCriticalHit);
		m_HitBoneId_DEPRECATED = other.HitBoneId_DEPRECATED;
		m_DamageTaken = other.DamageTaken;
		m_HealthRemaining = other.HealthRemaining;
		m_DamageSource = other.DamageSource;
		m_DamageType = other.DamageType;
		m_DamageResult = other.DamageResult;
		Helper.Set<LogPlayerUseWeaponData, LogPlayerUseWeaponDataInternal>(other.PlayerUseWeaponData, ref m_PlayerUseWeaponData);
		m_TimeSincePlayerUseWeaponMs = other.TimeSincePlayerUseWeaponMs;
		Helper.Set<Vec3f, Vec3fInternal>(other.DamagePosition, ref m_DamagePosition);
		Helper.Set<Vec3f, Vec3fInternal>(other.AttackerPlayerViewPosition, ref m_AttackerPlayerViewPosition);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_VictimPlayerPosition);
		Helper.Dispose(ref m_VictimPlayerViewRotation);
		Helper.Dispose(ref m_AttackerPlayerPosition);
		Helper.Dispose(ref m_AttackerPlayerViewRotation);
		Helper.Dispose(ref m_PlayerUseWeaponData);
		Helper.Dispose(ref m_DamagePosition);
		Helper.Dispose(ref m_AttackerPlayerViewPosition);
	}
}
