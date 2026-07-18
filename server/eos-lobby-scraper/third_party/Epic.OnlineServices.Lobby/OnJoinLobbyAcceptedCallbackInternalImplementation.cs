namespace Epic.OnlineServices.Lobby;

internal static class OnJoinLobbyAcceptedCallbackInternalImplementation
{
	private static OnJoinLobbyAcceptedCallbackInternal s_Delegate;

	public static OnJoinLobbyAcceptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinLobbyAcceptedCallbackInternal))]
	public static void EntryPoint(ref JoinLobbyAcceptedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<JoinLobbyAcceptedCallbackInfoInternal, OnJoinLobbyAcceptedCallback, JoinLobbyAcceptedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
