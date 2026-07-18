using System;

namespace Epic.OnlineServices.Stats;

internal struct StatInternal : IGettable<Stat>
{
	private int m_ApiVersion;

	private IntPtr m_Name;

	private long m_StartTime;

	private long m_EndTime;

	private int m_Value;

	public void Get(out Stat other)
	{
		other = default(Stat);
		Helper.Get(m_Name, out Utf8String to);
		other.Name = to;
		Helper.Get(m_StartTime, out DateTimeOffset? to2);
		other.StartTime = to2;
		Helper.Get(m_EndTime, out DateTimeOffset? to3);
		other.EndTime = to3;
		other.Value = m_Value;
	}
}
