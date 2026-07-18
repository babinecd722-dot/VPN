namespace Epic.OnlineServices.Ecom;

internal static class OnQueryEntitlementTokenCallbackInternalImplementation
{
	private static OnQueryEntitlementTokenCallbackInternal s_Delegate;

	public static OnQueryEntitlementTokenCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryEntitlementTokenCallbackInternal))]
	public static void EntryPoint(ref QueryEntitlementTokenCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryEntitlementTokenCallbackInfoInternal, OnQueryEntitlementTokenCallback, QueryEntitlementTokenCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
