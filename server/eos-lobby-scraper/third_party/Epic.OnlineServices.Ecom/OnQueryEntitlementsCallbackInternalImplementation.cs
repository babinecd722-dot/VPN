namespace Epic.OnlineServices.Ecom;

internal static class OnQueryEntitlementsCallbackInternalImplementation
{
	private static OnQueryEntitlementsCallbackInternal s_Delegate;

	public static OnQueryEntitlementsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryEntitlementsCallbackInternal))]
	public static void EntryPoint(ref QueryEntitlementsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryEntitlementsCallbackInfoInternal, OnQueryEntitlementsCallback, QueryEntitlementsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
