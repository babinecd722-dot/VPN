namespace Epic.OnlineServices.Lobby;

internal static class OnSendLobbyNativeInviteRequestedCallbackInternalImplementation
{
	private static OnSendLobbyNativeInviteRequestedCallbackInternal s_Delegate;

	public static OnSendLobbyNativeInviteRequestedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSendLobbyNativeInviteRequestedCallbackInternal))]
	public static void EntryPoint(ref SendLobbyNativeInviteRequestedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<SendLobbyNativeInviteRequestedCallbackInfoInternal, OnSendLobbyNativeInviteRequestedCallback, SendLobbyNativeInviteRequestedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
