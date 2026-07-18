using System;

namespace Epic.OnlineServices.TitleStorage;

internal struct ReadFileDataCallbackInfoInternal : ICallbackInfoInternal, IGettable<ReadFileDataCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_Filename;

	private uint m_TotalFileSizeBytes;

	private int m_IsLastChunk;

	private uint m_DataChunkLengthBytes;

	private IntPtr m_DataChunk;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out ReadFileDataCallbackInfo other)
	{
		other = default(ReadFileDataCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_Filename, out Utf8String to3);
		other.Filename = to3;
		other.TotalFileSizeBytes = m_TotalFileSizeBytes;
		Helper.Get(m_IsLastChunk, out bool to4);
		other.IsLastChunk = to4;
		Helper.Get(m_DataChunk, out ArraySegment<byte> to5, m_DataChunkLengthBytes);
		other.DataChunk = to5;
	}
}
