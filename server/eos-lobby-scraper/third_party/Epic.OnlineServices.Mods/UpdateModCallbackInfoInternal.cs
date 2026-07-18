using System;

namespace Epic.OnlineServices.Mods;

internal struct UpdateModCallbackInfoInternal : ICallbackInfoInternal, IGettable<UpdateModCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_LocalUserId;

	private IntPtr m_ClientData;

	private IntPtr m_Mod;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out UpdateModCallbackInfo other)
	{
		other = default(UpdateModCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_LocalUserId, out EpicAccountId to);
		other.LocalUserId = to;
		Helper.Get(m_ClientData, out object to2);
		other.ClientData = to2;
		Helper.Get<ModIdentifierInternal, ModIdentifier>(m_Mod, out ModIdentifier? to3);
		other.Mod = to3;
	}
}
