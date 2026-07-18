using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct OnQueryLeaderboardRanksCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnQueryLeaderboardRanksCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LeaderboardId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnQueryLeaderboardRanksCompleteCallbackInfo other)
	{
		other = default(OnQueryLeaderboardRanksCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LeaderboardId, out Utf8String to2);
		other.LeaderboardId = to2;
	}
}
