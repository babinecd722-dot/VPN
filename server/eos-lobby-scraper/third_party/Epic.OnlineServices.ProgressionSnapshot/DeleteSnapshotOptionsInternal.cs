using System;

namespace Epic.OnlineServices.ProgressionSnapshot;

internal struct DeleteSnapshotOptionsInternal : ISettable<DeleteSnapshotOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	public void Set(ref DeleteSnapshotOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
	}
}
