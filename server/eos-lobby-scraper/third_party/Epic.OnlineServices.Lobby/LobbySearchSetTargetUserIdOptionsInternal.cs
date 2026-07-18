using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbySearchSetTargetUserIdOptionsInternal : ISettable<LobbySearchSetTargetUserIdOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	public void Set(ref LobbySearchSetTargetUserIdOptions other)
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
