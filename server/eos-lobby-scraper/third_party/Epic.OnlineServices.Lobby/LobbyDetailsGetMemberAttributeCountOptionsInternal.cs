using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsGetMemberAttributeCountOptionsInternal : ISettable<LobbyDetailsGetMemberAttributeCountOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	public void Set(ref LobbyDetailsGetMemberAttributeCountOptions other)
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
