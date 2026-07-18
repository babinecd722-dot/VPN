using System;

namespace Epic.OnlineServices.Sessions;

internal struct UnregisterPlayersCallbackInfoInternal : ICallbackInfoInternal, IGettable<UnregisterPlayersCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_UnregisteredPlayers;

	private uint m_UnregisteredPlayersCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out UnregisterPlayersCallbackInfo other)
	{
		other = default(UnregisterPlayersCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_UnregisteredPlayers, out ProductUserId[] to2, m_UnregisteredPlayersCount);
		other.UnregisteredPlayers = to2;
	}
}
