using System;

namespace Epic.OnlineServices.UI;

internal struct OnDisplaySettingsUpdatedCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnDisplaySettingsUpdatedCallbackInfo>
{
	private IntPtr m_ClientData;

	private int m_IsVisible;

	private int m_IsExclusiveInput;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnDisplaySettingsUpdatedCallbackInfo other)
	{
		other = default(OnDisplaySettingsUpdatedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_IsVisible, out bool to2);
		other.IsVisible = to2;
		Helper.Get(m_IsExclusiveInput, out bool to3);
		other.IsExclusiveInput = to3;
	}
}
