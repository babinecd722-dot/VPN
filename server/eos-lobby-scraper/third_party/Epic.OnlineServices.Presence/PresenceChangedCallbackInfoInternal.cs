using System;

namespace Epic.OnlineServices.Presence;

internal struct PresenceChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<PresenceChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_PresenceUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out PresenceChangedCallbackInfo other)
	{
		other = default(PresenceChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get(m_PresenceUserId, out EpicAccountId to3);
		other.PresenceUserId = to3;
	}
}
