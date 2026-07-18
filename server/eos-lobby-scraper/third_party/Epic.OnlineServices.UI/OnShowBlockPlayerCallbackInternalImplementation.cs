namespace Epic.OnlineServices.UI;

internal static class OnShowBlockPlayerCallbackInternalImplementation
{
	private static OnShowBlockPlayerCallbackInternal s_Delegate;

	public static OnShowBlockPlayerCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnShowBlockPlayerCallbackInternal))]
	public static void EntryPoint(ref OnShowBlockPlayerCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnShowBlockPlayerCallbackInfoInternal, OnShowBlockPlayerCallback, OnShowBlockPlayerCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
