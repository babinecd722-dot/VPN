using System;

namespace Epic.OnlineServices.Sessions;

internal struct JoinSessionAcceptedCallbackInfoInternal : ICallbackInfoInternal, IGettable<JoinSessionAcceptedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private ulong m_UiEventId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out JoinSessionAcceptedCallbackInfo other)
	{
		other = default(JoinSessionAcceptedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		other.UiEventId = m_UiEventId;
	}
}
