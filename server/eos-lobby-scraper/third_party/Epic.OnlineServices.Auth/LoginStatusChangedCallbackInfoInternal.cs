using System;

namespace Epic.OnlineServices.Auth;

internal struct LoginStatusChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LoginStatusChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private LoginStatus m_PrevStatus;

	private LoginStatus m_CurrentStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LoginStatusChangedCallbackInfo other)
	{
		other = default(LoginStatusChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		other.PrevStatus = m_PrevStatus;
		other.CurrentStatus = m_CurrentStatus;
	}
}
