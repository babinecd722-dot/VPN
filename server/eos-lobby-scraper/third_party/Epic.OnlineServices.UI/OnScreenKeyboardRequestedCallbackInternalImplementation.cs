namespace Epic.OnlineServices.UI;

internal static class OnScreenKeyboardRequestedCallbackInternalImplementation
{
	private static OnScreenKeyboardRequestedCallbackInternal s_Delegate;

	public static OnScreenKeyboardRequestedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnScreenKeyboardRequestedCallbackInternal))]
	public static void EntryPoint(ref OnScreenKeyboardRequestedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnScreenKeyboardRequestedCallbackInfoInternal, OnScreenKeyboardRequestedCallback, OnScreenKeyboardRequestedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
