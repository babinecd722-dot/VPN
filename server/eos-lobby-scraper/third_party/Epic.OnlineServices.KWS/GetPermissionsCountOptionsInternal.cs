using System;

namespace Epic.OnlineServices.KWS;

internal struct GetPermissionsCountOptionsInternal : ISettable<GetPermissionsCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	public void Set(ref GetPermissionsCountOptions other)
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
