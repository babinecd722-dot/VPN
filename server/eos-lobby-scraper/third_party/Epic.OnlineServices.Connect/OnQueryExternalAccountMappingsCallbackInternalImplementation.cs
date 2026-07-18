namespace Epic.OnlineServices.Connect;

internal static class OnQueryExternalAccountMappingsCallbackInternalImplementation
{
	private static OnQueryExternalAccountMappingsCallbackInternal s_Delegate;

	public static OnQueryExternalAccountMappingsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryExternalAccountMappingsCallbackInternal))]
	public static void EntryPoint(ref QueryExternalAccountMappingsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryExternalAccountMappingsCallbackInfoInternal, OnQueryExternalAccountMappingsCallback, QueryExternalAccountMappingsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
