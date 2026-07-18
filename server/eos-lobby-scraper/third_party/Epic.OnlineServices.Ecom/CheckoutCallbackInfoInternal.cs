using System;

namespace Epic.OnlineServices.Ecom;

internal struct CheckoutCallbackInfoInternal : ICallbackInfoInternal, IGettable<CheckoutCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TransactionId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out CheckoutCallbackInfo other)
	{
		other = default(CheckoutCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TransactionId, out Utf8String to3);
		other.TransactionId = to3;
	}
}
