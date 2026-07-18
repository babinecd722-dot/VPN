using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogPlayerUseAbilityOptionsInternal : ISettable<LogPlayerUseAbilityOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_PlayerHandle;

	private uint m_AbilityId;

	private uint m_AbilityDurationMs;

	private uint m_AbilityCooldownMs;

	public void Set(ref LogPlayerUseAbilityOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_PlayerHandle = other.PlayerHandle;
		m_AbilityId = other.AbilityId;
		m_AbilityDurationMs = other.AbilityDurationMs;
		m_AbilityCooldownMs = other.AbilityCooldownMs;
	}

	public void Dispose()
	{
	}
}
