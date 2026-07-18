namespace Epic.OnlineServices.Lobby;

internal static class OnLobbyMemberUpdateReceivedCallbackInternalImplementation
{
	private static OnLobbyMemberUpdateReceivedCallbackInternal s_Delegate;

	public static OnLobbyMemberUpdateReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLobbyMemberUpdateReceivedCallbackInternal))]
	public static void EntryPoint(ref LobbyMemberUpdateReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LobbyMemberUpdateReceivedCallbackInfoInternal, OnLobbyMemberUpdateReceivedCallback, LobbyMemberUpdateReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
