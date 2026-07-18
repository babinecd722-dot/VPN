namespace Epic.OnlineServices.Lobby;

internal static class LobbySearchOnFindCallbackInternalImplementation
{
	private static LobbySearchOnFindCallbackInternal s_Delegate;

	public static LobbySearchOnFindCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(LobbySearchOnFindCallbackInternal))]
	public static void EntryPoint(ref LobbySearchFindCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<LobbySearchFindCallbackInfoInternal, LobbySearchOnFindCallback, LobbySearchFindCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
