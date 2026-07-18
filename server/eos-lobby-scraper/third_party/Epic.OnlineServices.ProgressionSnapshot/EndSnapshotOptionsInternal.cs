using System;

namespace Epic.OnlineServices.ProgressionSnapshot;

internal struct EndSnapshotOptionsInternal : ISettable<EndSnapshotOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_SnapshotId;

	public void Set(ref EndSnapshotOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_SnapshotId = other.SnapshotId;
	}

	public void Dispose()
	{
	}
}
