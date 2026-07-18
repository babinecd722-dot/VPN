namespace Epic.OnlineServices.Lobby;

internal static class OnLobbyInviteAcceptedCallbackInternalImplementation
{
	private static OnLobbyInviteAcceptedCallbackInternal s_Delegate;

	public static OnLobbyInviteAcceptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLobbyInviteAcceptedCallbackInternal))]
	public static void EntryPoint(ref LobbyInviteAcceptedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LobbyInviteAcceptedCallbackInfoInternal, OnLobbyInviteAcceptedCallback, LobbyInviteAcceptedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
