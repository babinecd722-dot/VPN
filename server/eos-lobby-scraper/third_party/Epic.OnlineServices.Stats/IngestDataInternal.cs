using System;

namespace Epic.OnlineServices.Stats;

internal struct IngestDataInternal : ISettable<IngestData>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_StatName;

	private int m_IngestAmount;

	public void Set(ref IngestData other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.StatName, ref m_StatName);
		m_IngestAmount = other.IngestAmount;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_StatName);
	}
}
