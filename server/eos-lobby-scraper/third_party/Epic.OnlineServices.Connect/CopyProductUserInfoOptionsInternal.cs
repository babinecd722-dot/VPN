using System;

namespace Epic.OnlineServices.Connect;

internal struct CopyProductUserInfoOptionsInternal : ISettable<CopyProductUserInfoOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	public void Set(ref CopyProductUserInfoOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
	}
}
