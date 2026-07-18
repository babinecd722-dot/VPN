using System;

namespace Epic.OnlineServices.Stats;

internal struct CopyStatByNameOptionsInternal : ISettable<CopyStatByNameOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private IntPtr m_Name;

	public void Set(ref CopyStatByNameOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set(other.Name, ref m_Name);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
		Helper.Dispose(ref m_Name);
	}
}
