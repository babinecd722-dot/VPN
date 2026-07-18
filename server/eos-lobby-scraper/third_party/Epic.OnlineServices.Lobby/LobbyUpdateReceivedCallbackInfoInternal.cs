using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbyUpdateReceivedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LobbyUpdateReceivedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LobbyId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LobbyUpdateReceivedCallbackInfo other)
	{
		other = default(LobbyUpdateReceivedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LobbyId, out Utf8String to2);
		other.LobbyId = to2;
	}
}
