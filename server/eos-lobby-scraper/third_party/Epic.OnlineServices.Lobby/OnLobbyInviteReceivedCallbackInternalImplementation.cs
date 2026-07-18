namespace Epic.OnlineServices.Lobby;

internal static class OnLobbyInviteReceivedCallbackInternalImplementation
{
	private static OnLobbyInviteReceivedCallbackInternal s_Delegate;

	public static OnLobbyInviteReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLobbyInviteReceivedCallbackInternal))]
	public static void EntryPoint(ref LobbyInviteReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LobbyInviteReceivedCallbackInfoInternal, OnLobbyInviteReceivedCallback, LobbyInviteReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
