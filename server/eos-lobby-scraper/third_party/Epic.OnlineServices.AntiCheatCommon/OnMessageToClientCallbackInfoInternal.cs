using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct OnMessageToClientCallbackInfoInternal : ICallbackInfoInternal, IGettable<OnMessageToClientCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_ClientHandle;

	private IntPtr m_MessageData;

	private uint m_MessageDataSizeBytes;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out OnMessageToClientCallbackInfo other)
	{
		other = default(OnMessageToClientCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		other.ClientHandle = m_ClientHandle;
		Helper.Get(m_MessageData, out ArraySegment<byte> to2, m_MessageDataSizeBytes);
		other.MessageData = to2;
	}
}
