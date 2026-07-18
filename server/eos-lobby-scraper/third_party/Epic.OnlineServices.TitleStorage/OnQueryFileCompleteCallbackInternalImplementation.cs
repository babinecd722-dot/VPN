namespace Epic.OnlineServices.TitleStorage;

internal static class OnQueryFileCompleteCallbackInternalImplementation
{
	private static OnQueryFileCompleteCallbackInternal s_Delegate;

	public static OnQueryFileCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryFileCompleteCallbackInternal))]
	public static void EntryPoint(ref QueryFileCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryFileCallbackInfoInternal, OnQueryFileCompleteCallback, QueryFileCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
