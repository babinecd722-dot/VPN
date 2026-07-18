using System;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct QueryFileOptionsInternal : ISettable<QueryFileOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Filename;

	public void Set(ref QueryFileOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.Filename, ref m_Filename);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Filename);
	}
}
