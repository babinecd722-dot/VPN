namespace Epic.OnlineServices.RTCAudio;

internal static class OnUpdateSendingVolumeCallbackInternalImplementation
{
	private static OnUpdateSendingVolumeCallbackInternal s_Delegate;

	public static OnUpdateSendingVolumeCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUpdateSendingVolumeCallbackInternal))]
	public static void EntryPoint(ref UpdateSendingVolumeCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UpdateSendingVolumeCallbackInfoInternal, OnUpdateSendingVolumeCallback, UpdateSendingVolumeCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
