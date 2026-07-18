namespace Epic.OnlineServices.Lobby;

internal static class OnHardMuteMemberCallbackInternalImplementation
{
	private static OnHardMuteMemberCallbackInternal s_Delegate;

	public static OnHardMuteMemberCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnHardMuteMemberCallbackInternal))]
	public static void EntryPoint(ref HardMuteMemberCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<HardMuteMemberCallbackInfoInternal, OnHardMuteMemberCallback, HardMuteMemberCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
