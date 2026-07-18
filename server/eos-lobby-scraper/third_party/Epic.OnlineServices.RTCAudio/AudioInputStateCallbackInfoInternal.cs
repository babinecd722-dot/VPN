using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AudioInputStateCallbackInfoInternal : ICallbackInfoInternal, IGettable<AudioInputStateCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private RTCAudioInputStatus m_Status;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out AudioInputStateCallbackInfo other)
	{
		other = default(AudioInputStateCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		other.Status = m_Status;
	}
}
