using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsCopyMemberAttributeByKeyOptionsInternal : ISettable<LobbyDetailsCopyMemberAttributeByKeyOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_TargetUserId;

	private IntPtr m_AttrKey;

	public void Set(ref LobbyDetailsCopyMemberAttributeByKeyOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.TargetUserId, ref m_TargetUserId);
		Helper.Set(other.AttrKey, ref m_AttrKey);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_TargetUserId);
		Helper.Dispose(ref m_AttrKey);
	}
}
