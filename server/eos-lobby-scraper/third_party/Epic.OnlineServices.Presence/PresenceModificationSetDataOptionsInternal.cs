using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationSetDataOptionsInternal : ISettable<PresenceModificationSetDataOptions>, IDisposable
{
	private int m_ApiVersion;

	private int m_RecordsCount;

	private IntPtr m_Records;

	public void Set(ref PresenceModificationSetDataOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<DataRecord, DataRecordInternal>(other.Records, ref m_Records, out m_RecordsCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Records);
	}
}
