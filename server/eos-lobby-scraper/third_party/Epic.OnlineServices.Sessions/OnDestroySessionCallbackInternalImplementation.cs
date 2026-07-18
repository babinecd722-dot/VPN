namespace Epic.OnlineServices.Sessions;

internal static class OnDestroySessionCallbackInternalImplementation
{
	private static OnDestroySessionCallbackInternal s_Delegate;

	public static OnDestroySessionCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDestroySessionCallbackInternal))]
	public static void EntryPoint(ref DestroySessionCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DestroySessionCallbackInfoInternal, OnDestroySessionCallback, DestroySessionCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
