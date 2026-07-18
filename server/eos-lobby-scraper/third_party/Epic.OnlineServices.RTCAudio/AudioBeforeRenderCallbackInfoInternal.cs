using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AudioBeforeRenderCallbackInfoInternal : ICallbackInfoInternal, IGettable<AudioBeforeRenderCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_Buffer;

	private IntPtr m_ParticipantId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out AudioBeforeRenderCallbackInfo other)
	{
		other = default(AudioBeforeRenderCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		Helper.Get<AudioBufferInternal, AudioBuffer>(m_Buffer, out AudioBuffer? to4);
		other.Buffer = to4;
		Helper.Get(m_ParticipantId, out ProductUserId to5);
		other.ParticipantId = to5;
	}
}
