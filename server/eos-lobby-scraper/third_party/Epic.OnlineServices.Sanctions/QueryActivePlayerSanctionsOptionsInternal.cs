using System;

namespace Epic.OnlineServices.Sanctions;

internal struct QueryActivePlayerSanctionsOptionsInternal : ISettable<QueryActivePlayerSanctionsOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private IntPtr m_LocalUserId;

	public void Set(ref QueryActivePlayerSanctionsOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
		Helper.Dispose(ref m_LocalUserId);
	}
}
