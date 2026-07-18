using System;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct FileMetadataInternal : IGettable<FileMetadata>
{
	private int m_ApiVersion;

	private uint m_FileSizeBytes;

	private IntPtr m_MD5Hash;

	private IntPtr m_Filename;

	private long m_LastModifiedTime;

	private uint m_UnencryptedDataSizeBytes;

	public void Get(out FileMetadata other)
	{
		other = default(FileMetadata);
		other.FileSizeBytes = m_FileSizeBytes;
		Helper.Get(m_MD5Hash, out Utf8String to);
		other.MD5Hash = to;
		Helper.Get(m_Filename, out Utf8String to2);
		other.Filename = to2;
		Helper.Get(m_LastModifiedTime, out DateTimeOffset? to3);
		other.LastModifiedTime = to3;
		other.UnencryptedDataSizeBytes = m_UnencryptedDataSizeBytes;
	}
}
