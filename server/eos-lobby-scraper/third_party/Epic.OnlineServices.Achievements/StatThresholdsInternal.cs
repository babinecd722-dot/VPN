using System;

namespace Epic.OnlineServices.Achievements;

internal struct StatThresholdsInternal : IGettable<StatThresholds>
{
	private int m_ApiVersion;

	private IntPtr m_Name;

	private int m_Threshold;

	public void Get(out StatThresholds other)
	{
		other = default(StatThresholds);
		Helper.Get(m_Name, out Utf8String to);
		other.Name = to;
		other.Threshold = m_Threshold;
	}
}
