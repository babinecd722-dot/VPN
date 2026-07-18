using System;

namespace Epic.OnlineServices.Stats;

internal struct QueryStatsOptionsInternal : ISettable<QueryStatsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private long m_StartTime;

	private long m_EndTime;

	private IntPtr m_StatNames;

	private uint m_StatNamesCount;

	private IntPtr m_TargetUserId;

	public void Set(ref QueryStatsOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.StartTime, ref m_StartTime);
		Helper.Set(other.EndTime, ref m_EndTime);
		Helper.Set(other.StatNames, ref m_StatNames, out m_StatNamesCount, isArrayItemAllocated: true);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_StatNames);
		Helper.Dispose(ref m_TargetUserId);
	}
}
