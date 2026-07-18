using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogGameRoundStartOptionsInternal : ISettable<LogGameRoundStartOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionIdentifier;

	private IntPtr m_LevelName;

	private IntPtr m_ModeName;

	private uint m_RoundTimeSeconds;

	private AntiCheatCommonGameRoundCompetitionType m_CompetitionType;

	public void Set(ref LogGameRoundStartOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set(other.SessionIdentifier, ref m_SessionIdentifier);
		Helper.Set(other.LevelName, ref m_LevelName);
		Helper.Set(other.ModeName, ref m_ModeName);
		m_RoundTimeSeconds = other.RoundTimeSeconds;
		m_CompetitionType = other.CompetitionType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionIdentifier);
		Helper.Dispose(ref m_LevelName);
		Helper.Dispose(ref m_ModeName);
	}
}
