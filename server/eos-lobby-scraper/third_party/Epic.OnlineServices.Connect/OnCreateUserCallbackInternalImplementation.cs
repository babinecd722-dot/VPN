namespace Epic.OnlineServices.Connect;

internal static class OnCreateUserCallbackInternalImplementation
{
	private static OnCreateUserCallbackInternal s_Delegate;

	public static OnCreateUserCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnCreateUserCallbackInternal))]
	public static void EntryPoint(ref CreateUserCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<CreateUserCallbackInfoInternal, OnCreateUserCallback, CreateUserCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
