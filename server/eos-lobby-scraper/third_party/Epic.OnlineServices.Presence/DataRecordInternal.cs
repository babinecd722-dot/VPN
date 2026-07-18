using System;

namespace Epic.OnlineServices.Presence;

internal struct DataRecordInternal : IGettable<DataRecord>, ISettable<DataRecord>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	private IntPtr m_Value;

	public void Get(out DataRecord other)
	{
		other = default(DataRecord);
		Helper.Get(m_Key, out Utf8String to);
		other.Key = to;
		Helper.Get(m_Value, out Utf8String to2);
		other.Value = to2;
	}

	public void Set(ref DataRecord other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Key, ref m_Key);
		Helper.Set(other.Value, ref m_Value);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Key);
		Helper.Dispose(ref m_Value);
	}
}
