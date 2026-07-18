using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct OnClientIntegrityViolatedCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnClientIntegrityViolatedCallbackInfo>
{
	private IntPtr m_ClientData;

	private AntiCheatClientViolationType m_ViolationType;

	private IntPtr m_ViolationMessage;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnClientIntegrityViolatedCallbackInfo other)
	{
		other = default(OnClientIntegrityViolatedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.ViolationType = m_ViolationType;
		Helper.Get(m_ViolationMessage, out Utf8String to2);
		other.ViolationMessage = to2;
	}
}
