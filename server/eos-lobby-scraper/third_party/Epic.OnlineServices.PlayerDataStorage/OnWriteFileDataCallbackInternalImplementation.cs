using System;

namespace Epic.OnlineServices.PlayerDataStorage;

internal static class OnWriteFileDataCallbackInternalImplementation
{
	private static OnWriteFileDataCallbackInternal s_Delegate;

	public static OnWriteFileDataCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnWriteFileDataCallbackInternal))]
	public static WriteResult EntryPoint(ref WriteFileDataCallbackInfoInternal data, IntPtr outDataBuffer, out uint outDataWritten)
	{
		if (Helper.TryGetStructCallback<WriteFileDataCallbackInfoInternal, OnWriteFileDataCallback, WriteFileDataCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			ArraySegment<byte> outDataBuffer2 = default(ArraySegment<byte>);
			WriteResult result = callback(ref callbackInfo, out outDataBuffer2);
			Helper.Get(outDataBuffer2, out outDataWritten);
			Helper.Copy(outDataBuffer2, outDataBuffer);
			return result;
		}
		outDataWritten = 0u;
		return (WriteResult)0;
	}
}
