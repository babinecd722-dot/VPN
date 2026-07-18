namespace Epic.OnlineServices.Lobby;

internal static class OnLobbyInviteRejectedCallbackInternalImplementation
{
	private static OnLobbyInviteRejectedCallbackInternal s_Delegate;

	public static OnLobbyInviteRejectedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLobbyInviteRejectedCallbackInternal))]
	public static void EntryPoint(ref LobbyInviteRejectedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LobbyInviteRejectedCallbackInfoInternal, OnLobbyInviteRejectedCallback, LobbyInviteRejectedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
