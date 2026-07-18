namespace Epic.OnlineServices.KWS;

internal static class OnUpdateParentEmailCallbackInternalImplementation
{
	private static OnUpdateParentEmailCallbackInternal s_Delegate;

	public static OnUpdateParentEmailCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateParentEmailCallbackInternal))]
	public static void EntryPoint(ref UpdateParentEmailCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateParentEmailCallbackInfoInternal, OnUpdateParentEmailCallback, UpdateParentEmailCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
