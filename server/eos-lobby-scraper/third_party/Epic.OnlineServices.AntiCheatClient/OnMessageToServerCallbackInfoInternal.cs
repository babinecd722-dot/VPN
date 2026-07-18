using System;

namespace Epic.OnlineServices.AntiCheatClient;

internal struct OnMessageToServerCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnMessageToServerCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_MessageData;

	private uint m_MessageDataSizeBytes;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnMessageToServerCallbackInfo other)
	{
		other = default(OnMessageToServerCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_MessageData, out ArraySegment<byte> to2, m_MessageDataSizeBytes);
		other.MessageData = to2;
	}
}
