namespace Epic.OnlineServices.Ecom;

internal static class OnQueryOffersCallbackInternalImplementation
{
	private static OnQueryOffersCallbackInternal s_Delegate;

	public static OnQueryOffersCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryOffersCallbackInternal))]
	public static void EntryPoint(ref QueryOffersCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryOffersCallbackInfoInternal, OnQueryOffersCallback, QueryOffersCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
