using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct OnClientAuthStatusChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnClientAuthStatusChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_ClientHandle;

	private AntiCheatCommonClientAuthStatus m_ClientAuthStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnClientAuthStatusChangedCallbackInfo other)
	{
		other = default(OnClientAuthStatusChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.ClientHandle = m_ClientHandle;
		other.ClientAuthStatus = m_ClientAuthStatus;
	}
}
