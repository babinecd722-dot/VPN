namespace Epic.OnlineServices.Connect;

internal static class OnTransferDeviceIdAccountCallbackInternalImplementation
{
	private static OnTransferDeviceIdAccountCallbackInternal s_Delegate;

	public static OnTransferDeviceIdAccountCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnTransferDeviceIdAccountCallbackInternal))]
	public static void EntryPoint(ref TransferDeviceIdAccountCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<TransferDeviceIdAccountCallbackInfoInternal, OnTransferDeviceIdAccountCallback, TransferDeviceIdAccountCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
