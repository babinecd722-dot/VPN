namespace Epic.OnlineServices.Sessions;

internal static class OnStartSessionCallbackInternalImplementation
{
	private static OnStartSessionCallbackInternal s_Delegate;

	public static OnStartSessionCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnStartSessionCallbackInternal))]
	public static void EntryPoint(ref StartSessionCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<StartSessionCallbackInfoInternal, OnStartSessionCallback, StartSessionCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
