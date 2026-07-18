namespace Epic.OnlineServices.Lobby;

internal static class OnKickMemberCallbackInternalImplementation
{
	private static OnKickMemberCallbackInternal s_Delegate;

	public static OnKickMemberCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnKickMemberCallbackInternal))]
	public static void EntryPoint(ref KickMemberCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<KickMemberCallbackInfoInternal, OnKickMemberCallback, KickMemberCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
