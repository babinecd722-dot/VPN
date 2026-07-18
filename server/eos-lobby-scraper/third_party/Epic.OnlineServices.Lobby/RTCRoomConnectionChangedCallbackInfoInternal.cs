using System;

namespace Epic.OnlineServices.Lobby;

internal struct RTCRoomConnectionChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<RTCRoomConnectionChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LobbyId;

	private IntPtr m_LocalUserId;

	private int m_IsConnected;

	private Result m_DisconnectReason;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out RTCRoomConnectionChangedCallbackInfo other)
	{
		other = default(RTCRoomConnectionChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LobbyId, out Utf8String to2);
		other.LobbyId = to2;
		Helper.Get(m_LocalUserId, out ProductUserId to3);
		other.LocalUserId = to3;
		Helper.Get(m_IsConnected, out bool to4);
		other.IsConnected = to4;
		other.DisconnectReason = m_DisconnectReason;
	}
}
