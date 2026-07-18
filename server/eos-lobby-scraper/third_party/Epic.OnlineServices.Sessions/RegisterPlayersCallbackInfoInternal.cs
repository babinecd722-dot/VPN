using System;

namespace Epic.OnlineServices.Sessions;

internal struct RegisterPlayersCallbackInfoInternal : ICallbackInfoInternal, IGettable<RegisterPlayersCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_RegisteredPlayers;

	private uint m_RegisteredPlayersCount;

	private IntPtr m_SanctionedPlayers;

	private uint m_SanctionedPlayersCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out RegisterPlayersCallbackInfo other)
	{
		other = default(RegisterPlayersCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_RegisteredPlayers, out ProductUserId[] to2, m_RegisteredPlayersCount);
		other.RegisteredPlayers = to2;
		Helper.Get(m_SanctionedPlayers, out ProductUserId[] to3, m_SanctionedPlayersCount);
		other.SanctionedPlayers = to3;
	}
}
