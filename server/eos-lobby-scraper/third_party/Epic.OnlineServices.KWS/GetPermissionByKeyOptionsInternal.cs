using System;

namespace Epic.OnlineServices.KWS;

internal struct GetPermissionByKeyOptionsInternal : ISettable<GetPermissionByKeyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Key;

	public void Set(ref GetPermissionByKeyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.Key, ref m_Key);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Key);
	}
}
