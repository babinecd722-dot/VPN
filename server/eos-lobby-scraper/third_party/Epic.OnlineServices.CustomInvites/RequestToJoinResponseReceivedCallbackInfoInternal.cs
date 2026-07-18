using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct RequestToJoinResponseReceivedCallbackInfoInternal : ICallbackInfoInternal, IGettable<RequestToJoinResponseReceivedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_FromUserId;

	private IntPtr m_ToUserId;

	private RequestToJoinResponse m_Response;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out RequestToJoinResponseReceivedCallbackInfo other)
	{
		other = default(RequestToJoinResponseReceivedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_FromUserId, out ProductUserId to2);
		other.FromUserId = to2;
		Helper.Get(m_ToUserId, out ProductUserId to3);
		other.ToUserId = to3;
		other.Response = m_Response;
	}
}
