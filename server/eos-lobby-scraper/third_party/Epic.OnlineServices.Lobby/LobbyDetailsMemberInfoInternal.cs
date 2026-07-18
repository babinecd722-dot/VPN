using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyDetailsMemberInfoInternal : IGettable<LobbyDetailsMemberInfo>
{
	private int m_ApiVersion;

	private IntPtr m_UserId;

	private uint m_Platform;

	private int m_AllowsCrossplay;

	public void Get(out LobbyDetailsMemberInfo other)
	{
		other = default(LobbyDetailsMemberInfo);
		Helper.Get(m_UserId, out ProductUserId to);
		other.UserId = to;
		other.Platform = m_Platform;
		Helper.Get(m_AllowsCrossplay, out bool to2);
		other.AllowsCrossplay = to2;
	}
}
