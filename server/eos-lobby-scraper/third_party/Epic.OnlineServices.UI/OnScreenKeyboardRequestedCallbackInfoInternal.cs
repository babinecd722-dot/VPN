using System;

namespace Epic.OnlineServices.UI;

internal struct OnScreenKeyboardRequestedCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnScreenKeyboardRequestedCallbackInfo>
{
	private IntPtr m_ClientData;

	private OnScreenKeyboardType m_Type;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnScreenKeyboardRequestedCallbackInfo other)
	{
		other = default(OnScreenKeyboardRequestedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.Type = m_Type;
	}
}
