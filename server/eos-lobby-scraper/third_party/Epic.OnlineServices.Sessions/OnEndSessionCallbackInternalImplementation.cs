namespace Epic.OnlineServices.Sessions;

internal static class OnEndSessionCallbackInternalImplementation
{
	private static OnEndSessionCallbackInternal s_Delegate;

	public static OnEndSessionCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnEndSessionCallbackInternal))]
	public static void EntryPoint(ref EndSessionCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<EndSessionCallbackInfoInternal, OnEndSessionCallback, EndSessionCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
