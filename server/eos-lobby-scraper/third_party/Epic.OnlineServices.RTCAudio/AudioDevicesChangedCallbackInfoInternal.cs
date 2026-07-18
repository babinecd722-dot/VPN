using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AudioDevicesChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<AudioDevicesChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out AudioDevicesChangedCallbackInfo other)
	{
		other = default(AudioDevicesChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
