using System;

namespace Epic.OnlineServices.Lobby;

internal struct LobbySearchFindCallbackInfoInternal : ICallbackInfoInternal, IGettable<LobbySearchFindCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LobbySearchFindCallbackInfo other)
	{
		other = default(LobbySearchFindCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
