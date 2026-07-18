using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct LogPlayerUseWeaponData
{
	public IntPtr PlayerHandle { get; set; }

	public Vec3f? PlayerPosition { get; set; }

	public Quat? PlayerViewRotation { get; set; }

	public bool IsPlayerViewZoomed { get; set; }

	public bool IsMeleeAttack { get; set; }

	public Utf8String WeaponName { get; set; }
}
