using System;

namespace Epic.OnlineServices.Achievements;

internal struct PlayerStatInfoInternal : IGettable<PlayerStatInfo>
{
	private int m_ApiVersion;

	private IntPtr m_Name;

	private int m_CurrentValue;

	private int m_ThresholdValue;

	public void Get(out PlayerStatInfo other)
	{
		other = default(PlayerStatInfo);
		Helper.Get(m_Name, out Utf8String to);
		other.Name = to;
		other.CurrentValue = m_CurrentValue;
		other.ThresholdValue = m_ThresholdValue;
	}
}
