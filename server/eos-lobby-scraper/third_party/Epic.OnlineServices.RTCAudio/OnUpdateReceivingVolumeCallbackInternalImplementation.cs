namespace Epic.OnlineServices.RTCAudio;

internal static class OnUpdateReceivingVolumeCallbackInternalImplementation
{
	private static OnUpdateReceivingVolumeCallbackInternal s_Delegate;

	public static OnUpdateReceivingVolumeCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateReceivingVolumeCallbackInternal))]
	public static void EntryPoint(ref UpdateReceivingVolumeCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateReceivingVolumeCallbackInfoInternal, OnUpdateReceivingVolumeCallback, UpdateReceivingVolumeCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
