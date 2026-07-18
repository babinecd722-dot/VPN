using System;

namespace Epic.OnlineServices.Lobby;

internal struct JoinLobbyAcceptedCallbackInfoInternal : ICallbackInfoInternal, IGettable<JoinLobbyAcceptedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private ulong m_UiEventId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out JoinLobbyAcceptedCallbackInfo other)
	{
		other = default(JoinLobbyAcceptedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		other.UiEventId = m_UiEventId;
	}
}
