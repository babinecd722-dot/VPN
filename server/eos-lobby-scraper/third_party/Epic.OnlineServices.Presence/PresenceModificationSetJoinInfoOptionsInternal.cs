using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceModificationSetJoinInfoOptionsInternal : ISettable<PresenceModificationSetJoinInfoOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_JoinInfo;

	public void Set(ref PresenceModificationSetJoinInfoOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.JoinInfo, ref m_JoinInfo);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_JoinInfo);
	}
}
