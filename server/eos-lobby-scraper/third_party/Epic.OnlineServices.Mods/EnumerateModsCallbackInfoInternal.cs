using System;

namespace Epic.OnlineServices.Mods;

internal struct EnumerateModsCallbackInfoInternal : ICallbackInfoInternal, IGettable<EnumerateModsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_LocalUserId;

	private IntPtr m_ClientData;

	private ModEnumerationType m_Type;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out EnumerateModsCallbackInfo other)
	{
		other = default(EnumerateModsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_LocalUserId, out EpicAccountId to);
		other.LocalUserId = to;
		Helper.Get(m_ClientData, out object to2);
		other.ClientData = to2;
		other.Type = m_Type;
	}
}
