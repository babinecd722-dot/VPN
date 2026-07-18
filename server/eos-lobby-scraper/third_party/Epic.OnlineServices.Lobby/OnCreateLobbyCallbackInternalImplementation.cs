namespace Epic.OnlineServices.Lobby;

internal static class OnCreateLobbyCallbackInternalImplementation
{
	private static OnCreateLobbyCallbackInternal s_Delegate;

	public static OnCreateLobbyCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnCreateLobbyCallbackInternal))]
	public static void EntryPoint(ref CreateLobbyCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<CreateLobbyCallbackInfoInternal, OnCreateLobbyCallback, CreateLobbyCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
