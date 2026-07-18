namespace Epic.OnlineServices.Lobby;

internal static class OnJoinLobbyByIdCallbackInternalImplementation
{
	private static OnJoinLobbyByIdCallbackInternal s_Delegate;

	public static OnJoinLobbyByIdCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinLobbyByIdCallbackInternal))]
	public static void EntryPoint(ref JoinLobbyByIdCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<JoinLobbyByIdCallbackInfoInternal, OnJoinLobbyByIdCallback, JoinLobbyByIdCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
