using System;

namespace Epic.OnlineServices.Lobby;

internal struct HardMuteMemberOptionsInternal : ISettable<HardMuteMemberOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LobbyId;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserId;

	private int m_HardMute;

	public void Set(ref HardMuteMemberOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.LobbyId, ref m_LobbyId);
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set(other.HardMute, ref m_HardMute);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LobbyId);
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_TargetUserId);
	}
}
