using System;

namespace Epic.OnlineServices.Lobby;

internal struct LeaveLobbyRequestedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LeaveLobbyRequestedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_LobbyId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LeaveLobbyRequestedCallbackInfo other)
	{
		other = default(LeaveLobbyRequestedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_LobbyId, out Utf8String to3);
		other.LobbyId = to3;
	}
}
