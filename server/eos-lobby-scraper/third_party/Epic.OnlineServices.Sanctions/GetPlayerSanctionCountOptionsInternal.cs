using System;

namespace Epic.OnlineServices.Sanctions;

internal struct GetPlayerSanctionCountOptionsInternal : ISettable<GetPlayerSanctionCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	public void Set(ref GetPlayerSanctionCountOptions other)
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
