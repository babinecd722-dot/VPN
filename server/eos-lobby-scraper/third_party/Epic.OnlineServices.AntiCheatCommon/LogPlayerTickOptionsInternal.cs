using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerTickOptionsInternal : ISettable<LogPlayerTickOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlayerHandle;

	private IntPtr m_PlayerPosition;

	private IntPtr m_PlayerViewRotation;

	private int m_IsPlayerViewZoomed;

	private float m_PlayerHealth;

	private AntiCheatCommonPlayerMovementState m_PlayerMovementState;

	private IntPtr m_PlayerViewPosition;

	public void Set(ref LogPlayerTickOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		m_PlayerHandle = other.PlayerHandle;
		Helper.Set<Vec3f, Vec3fInternal>(other.PlayerPosition, ref m_PlayerPosition);
		Helper.Set<Quat, QuatInternal>(other.PlayerViewRotation, ref m_PlayerViewRotation);
		Helper.Set(other.IsPlayerViewZoomed, ref m_IsPlayerViewZoomed);
		m_PlayerHealth = other.PlayerHealth;
		m_PlayerMovementState = other.PlayerMovementState;
		Helper.Set<Vec3f, Vec3fInternal>(other.PlayerViewPosition, ref m_PlayerViewPosition);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_PlayerPosition);
		Helper.Dispose(ref m_PlayerViewRotation);
		Helper.Dispose(ref m_PlayerViewPosition);
	}
}
