using System;

namespace Epic.OnlineServices.TitleStorage;

internal struct FileMetadataInternal : IGettable<FileMetadata>
{
	private int m_ApiVersion;

	private uint m_FileSizeBytes;

	private IntPtr m_MD5Hash;

	private IntPtr m_Filename;

	private uint m_UnencryptedDataSizeBytes;

	public void Get(out FileMetadata other)
	{
		other = default(FileMetadata);
		other.FileSizeBytes = m_FileSizeBytes;
		Helper.Get(m_MD5Hash, out Utf8String to);
		other.MD5Hash = to;
		Helper.Get(m_Filename, out Utf8String to2);
		other.Filename = to2;
		other.UnencryptedDataSizeBytes = m_UnencryptedDataSizeBytes;
	}
}
