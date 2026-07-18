namespace Epic.OnlineServices.TitleStorage;

internal static class OnDeleteCacheCompleteCallbackInternalImplementation
{
	private static OnDeleteCacheCompleteCallbackInternal s_Delegate;

	public static OnDeleteCacheCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDeleteCacheCompleteCallbackInternal))]
	public static void EntryPoint(ref DeleteCacheCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DeleteCacheCallbackInfoInternal, OnDeleteCacheCompleteCallback, DeleteCacheCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
