using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct OnClientActionRequiredCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnClientActionRequiredCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_ClientHandle;

	private AntiCheatCommonClientAction m_ClientAction;

	private AntiCheatCommonClientActionReason m_ActionReasonCode;

	private IntPtr m_ActionReasonDetailsString;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnClientActionRequiredCallbackInfo other)
	{
		other = default(OnClientActionRequiredCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.ClientHandle = m_ClientHandle;
		other.ClientAction = m_ClientAction;
		other.ActionReasonCode = m_ActionReasonCode;
		Helper.Get(m_ActionReasonDetailsString, out Utf8String to2);
		other.ActionReasonDetailsString = to2;
	}
}
