namespace Epic.OnlineServices.Auth;

internal static class OnDeletePersistentAuthCallbackInternalImplementation
{
	private static OnDeletePersistentAuthCallbackInternal s_Delegate;

	public static OnDeletePersistentAuthCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDeletePersistentAuthCallbackInternal))]
	public static void EntryPoint(ref DeletePersistentAuthCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<DeletePersistentAuthCallbackInfoInternal, OnDeletePersistentAuthCallback, DeletePersistentAuthCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
