namespace Epic.OnlineServices.Sessions;

internal static class OnJoinSessionCallbackInternalImplementation
{
	private static OnJoinSessionCallbackInternal s_Delegate;

	public static OnJoinSessionCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinSessionCallbackInternal))]
	public static void EntryPoint(ref JoinSessionCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<JoinSessionCallbackInfoInternal, OnJoinSessionCallback, JoinSessionCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
