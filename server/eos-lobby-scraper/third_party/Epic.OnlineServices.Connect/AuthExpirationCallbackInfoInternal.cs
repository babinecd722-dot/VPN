using System;

namespace Epic.OnlineServices.Connect;

internal struct AuthExpirationCallbackInfoInternal : ICallbackInfoInternal, IGettable<AuthExpirationCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out AuthExpirationCallbackInfo other)
	{
		other = default(AuthExpirationCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
	}
}
