namespace Epic.OnlineServices.Leaderboards;

internal static class OnQueryLeaderboardDefinitionsCompleteCallbackInternalImplementation
{
	private static OnQueryLeaderboardDefinitionsCompleteCallbackInternal s_Delegate;

	public static OnQueryLeaderboardDefinitionsCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryLeaderboardDefinitionsCompleteCallbackInternal))]
	public static void EntryPoint(ref OnQueryLeaderboardDefinitionsCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryLeaderboardDefinitionsCompleteCallbackInfoInternal, OnQueryLeaderboardDefinitionsCompleteCallback, OnQueryLeaderboardDefinitionsCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
