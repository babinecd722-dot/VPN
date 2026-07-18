namespace Epic.OnlineServices.Leaderboards;

internal static class OnQueryLeaderboardRanksCompleteCallbackInternalImplementation
{
	private static OnQueryLeaderboardRanksCompleteCallbackInternal s_Delegate;

	public static OnQueryLeaderboardRanksCompleteCallbackInternal Delegate
	{
		get
		{
			if (s_Delegate == null)
			{
				s_Delegate = EntryPoint;
			}
			return s_Delegate;
		}
	}

	[MonoPInvokeCallback(typeof(OnQueryLeaderboardRanksCompleteCallbackInternal))]
	public static void EntryPoint(ref OnQueryLeaderboardRanksCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryLeaderboardRanksCompleteCallbackInfoInternal, OnQueryLeaderboardRanksCompleteCallback, OnQueryLeaderboardRanksCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
