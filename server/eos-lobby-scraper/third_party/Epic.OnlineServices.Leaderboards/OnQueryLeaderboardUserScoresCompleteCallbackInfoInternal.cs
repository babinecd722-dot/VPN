using System;

namespace Epic.OnlineServices.Leaderboards;

internal struct OnQueryLeaderboardUserScoresCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnQueryLeaderboardUserScoresCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnQueryLeaderboardUserScoresCompleteCallbackInfo other)
	{
		other = default(OnQueryLeaderboardUserScoresCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
