namespace Epic.OnlineServices.Lobby;

internal static class OnJoinLobbyCallbackInternalImplementation
{
	private static OnJoinLobbyCallbackInternal s_Delegate;

	public static OnJoinLobbyCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinLobbyCallbackInternal))]
	public static void EntryPoint(ref JoinLobbyCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<JoinLobbyCallbackInfoInternal, OnJoinLobbyCallback, JoinLobbyCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
