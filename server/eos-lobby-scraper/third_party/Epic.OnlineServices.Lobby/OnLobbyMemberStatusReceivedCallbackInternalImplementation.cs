namespace Epic.OnlineServices.Lobby;

internal static class OnLobbyMemberStatusReceivedCallbackInternalImplementation
{
	private static OnLobbyMemberStatusReceivedCallbackInternal s_Delegate;

	public static OnLobbyMemberStatusReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLobbyMemberStatusReceivedCallbackInternal))]
	public static void EntryPoint(ref LobbyMemberStatusReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LobbyMemberStatusReceivedCallbackInfoInternal, OnLobbyMemberStatusReceivedCallback, LobbyMemberStatusReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
