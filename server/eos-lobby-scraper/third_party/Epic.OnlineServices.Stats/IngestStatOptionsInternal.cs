using System;

namespace Epic.OnlineServices.Stats;

internal struct IngestStatOptionsInternal : ISettable<IngestStatOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Stats;

	private uint m_StatsCount;

	private IntPtr m_TargetUserId;

	public void Set(ref IngestStatOptions other)
	{
		Dispose();
		m_ApiVersion = 3;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set<IngestData, IngestDataInternal>(other.Stats, ref m_Stats, out m_StatsCount, isArrayItemAllocated: false);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Stats);
		Helper.Dispose(ref m_TargetUserId);
	}
}
