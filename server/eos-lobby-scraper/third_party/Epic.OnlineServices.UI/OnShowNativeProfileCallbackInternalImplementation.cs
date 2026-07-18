namespace Epic.OnlineServices.UI;

internal static class OnShowNativeProfileCallbackInternalImplementation
{
	private static OnShowNativeProfileCallbackInternal s_Delegate;

	public static OnShowNativeProfileCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnShowNativeProfileCallbackInternal))]
	public static void EntryPoint(ref ShowNativeProfileCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<ShowNativeProfileCallbackInfoInternal, OnShowNativeProfileCallback, ShowNativeProfileCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
