namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnDuplicateFileCompleteCallbackInternalImplementation
{
	private static OnDuplicateFileCompleteCallbackInternal s_Delegate;

	public static OnDuplicateFileCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDuplicateFileCompleteCallbackInternal))]
	public static void EntryPoint(ref DuplicateFileCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DuplicateFileCallbackInfoInternal, OnDuplicateFileCompleteCallback, DuplicateFileCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
