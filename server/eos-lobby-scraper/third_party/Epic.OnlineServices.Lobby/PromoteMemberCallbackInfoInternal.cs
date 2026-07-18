using System;

namespace Epic.OnlineServices.Lobby;

internal struct PromoteMemberCallbackInfoInternal : ICallbackInfoInternal, IGettable<PromoteMemberCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LobbyId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out PromoteMemberCallbackInfo other)
	{
		other = default(PromoteMemberCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LobbyId, out Utf8String to2);
		other.LobbyId = to2;
	}
}
