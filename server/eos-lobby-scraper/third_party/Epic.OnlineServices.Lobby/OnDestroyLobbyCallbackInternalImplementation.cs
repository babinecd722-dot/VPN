namespace Epic.OnlineServices.Lobby;

internal static class OnDestroyLobbyCallbackInternalImplementation
{
	private static OnDestroyLobbyCallbackInternal s_Delegate;

	public static OnDestroyLobbyCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDestroyLobbyCallbackInternal))]
	public static void EntryPoint(ref DestroyLobbyCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DestroyLobbyCallbackInfoInternal, OnDestroyLobbyCallback, DestroyLobbyCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
