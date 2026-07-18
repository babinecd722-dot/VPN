namespace Epic.OnlineServices.Lobby;

internal static class OnLobbyUpdateReceivedCallbackInternalImplementation
{
	private static OnLobbyUpdateReceivedCallbackInternal s_Delegate;

	public static OnLobbyUpdateReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLobbyUpdateReceivedCallbackInternal))]
	public static void EntryPoint(ref LobbyUpdateReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LobbyUpdateReceivedCallbackInfoInternal, OnLobbyUpdateReceivedCallback, LobbyUpdateReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
