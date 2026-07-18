using System;

namespace Epic.OnlineServices.PlayerDataStorage;

public struct FileMetadata
{
	public uint FileSizeBytes { get; set; }

	public Utf8String MD5Hash { get; set; }

	public Utf8String Filename { get; set; }

	public DateTimeOffset? LastModifiedTime { get; set; }

	public uint UnencryptedDataSizeBytes { get; set; }
}
