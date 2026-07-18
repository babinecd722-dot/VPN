namespace Epic.OnlineServices.Ecom;

internal static class OnCheckoutCallbackInternalImplementation
{
	private static OnCheckoutCallbackInternal s_Delegate;

	public static OnCheckoutCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnCheckoutCallbackInternal))]
	public static void EntryPoint(ref CheckoutCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<CheckoutCallbackInfoInternal, OnCheckoutCallback, CheckoutCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
