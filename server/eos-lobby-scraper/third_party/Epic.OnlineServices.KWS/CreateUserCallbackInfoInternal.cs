using System;

namespace Epic.OnlineServices.KWS;

internal struct CreateUserCallbackInfoInternal : ICallbackInfoInternal, IGettable<CreateUserCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_KWSUserId;

	private int m_IsMinor;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out CreateUserCallbackInfo other)
	{
		other = default(CreateUserCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_KWSUserId, out Utf8String to3);
		other.KWSUserId = to3;
		Helper.Get(m_IsMinor, out bool to4);
		other.IsMinor = to4;
	}
}
