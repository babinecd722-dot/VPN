using System;

namespace Epic.OnlineServices.UI;

internal struct MemoryMonitorCallbackInfoInternal : ICallbackInfoInternal, IGettable<MemoryMonitorCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_SystemMemoryMonitorReport;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out MemoryMonitorCallbackInfo other)
	{
		other = default(MemoryMonitorCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.SystemMemoryMonitorReport = m_SystemMemoryMonitorReport;
	}
}
