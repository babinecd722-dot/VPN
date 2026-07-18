using System;

namespace Epic.OnlineServices.ProgressionSnapshot;

internal struct AddProgressionOptionsInternal : ISettable<AddProgressionOptions>, IDisposable
{
	private int m_ApiVersion;

	private uint m_SnapshotId;

	private IntPtr m_Key;

	private IntPtr m_Value;

	public void Set(ref AddProgressionOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		m_SnapshotId = other.SnapshotId;
		Helper.Set(other.Key, ref m_Key);
		Helper.Set(other.Value, ref m_Value);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Key);
		Helper.Dispose(ref m_Value);
	}
}
