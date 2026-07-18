namespace Epic.OnlineServices.Mods;

internal static class OnUpdateModCallbackInternalImplementation
{
	private static OnUpdateModCallbackInternal s_Delegate;

	public static OnUpdateModCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateModCallbackInternal))]
	public static void EntryPoint(ref UpdateModCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateModCallbackInfoInternal, OnUpdateModCallback, UpdateModCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
