using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationDeleteDataOptionsInternal : ISettable<PresenceModificationDeleteDataOptions>, IDisposable
{
	private int m_ApiVersion;

	private int m_RecordsCount;

	private IntPtr m_Records;

	public void Set(ref PresenceModificationDeleteDataOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<PresenceModificationDataRecordId, PresenceModificationDataRecordIdInternal>(other.Records, ref m_Records, out m_RecordsCount, isArrayItemAllocated: false);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Records);
	}
}
