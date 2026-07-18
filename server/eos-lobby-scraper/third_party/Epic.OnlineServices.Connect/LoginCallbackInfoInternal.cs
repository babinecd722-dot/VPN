using System;

namespace Epic.OnlineServices.Connect;

internal struct LoginCallbackInfoInternal : ICallbackInfoInternal, IGettable<LoginCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_ContinuanceToken;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LoginCallbackInfo other)
	{
		other = default(LoginCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_ContinuanceToken, out ContinuanceToken to3);
		other.ContinuanceToken = to3;
	}
}
