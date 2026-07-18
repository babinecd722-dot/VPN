namespace Epic.OnlineServices.Sessions;

internal static class OnUpdateSessionCallbackInternalImplementation
{
	private static OnUpdateSessionCallbackInternal s_Delegate;

	public static OnUpdateSessionCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateSessionCallbackInternal))]
	public static void EntryPoint(ref UpdateSessionCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateSessionCallbackInfoInternal, OnUpdateSessionCallback, UpdateSessionCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
