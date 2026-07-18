namespace Epic.OnlineServices.Leaderboards;

internal static class OnQueryLeaderboardUserScoresCompleteCallbackInternalImplementation
{
	private static OnQueryLeaderboardUserScoresCompleteCallbackInternal s_Delegate;

	public static OnQueryLeaderboardUserScoresCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryLeaderboardUserScoresCompleteCallbackInternal))]
	public static void EntryPoint(ref OnQueryLeaderboardUserScoresCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryLeaderboardUserScoresCompleteCallbackInfoInternal, OnQueryLeaderboardUserScoresCompleteCallback, OnQueryLeaderboardUserScoresCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
