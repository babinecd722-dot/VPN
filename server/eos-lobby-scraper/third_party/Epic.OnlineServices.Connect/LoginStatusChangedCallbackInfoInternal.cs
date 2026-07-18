using System;

namespace Epic.OnlineServices.Connect;

internal struct LoginStatusChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<LoginStatusChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private LoginStatus m_PreviousStatus;

	private LoginStatus m_CurrentStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LoginStatusChangedCallbackInfo other)
	{
		other = default(LoginStatusChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		other.PreviousStatus = m_PreviousStatus;
		other.CurrentStatus = m_CurrentStatus;
	}
}
