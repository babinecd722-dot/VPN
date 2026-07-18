using System;

namespace Epic.OnlineServices.PlayerDataStorage;

public sealed class PlayerDataStorageInterface : Handle
{
	public const int COPYFILEMETADATAATINDEXOPTIONS_API_LATEST = 1;

	public const int COPYFILEMETADATAATINDEX_API_LATEST = 1;

	public const int COPYFILEMETADATABYFILENAMEOPTIONS_API_LATEST = 1;

	public const int COPYFILEMETADATABYFILENAME_API_LATEST = 1;

	public const int DELETECACHEOPTIONS_API_LATEST = 1;

	public const int DELETECACHE_API_LATEST = 1;

	public const int DELETEFILEOPTIONS_API_LATEST = 1;

	public const int DELETEFILE_API_LATEST = 1;

	public const int DUPLICATEFILEOPTIONS_API_LATEST = 1;

	public const int DUPLICATEFILE_API_LATEST = 1;

	public const int FILEMETADATA_API_LATEST = 3;

	public const int FILENAME_MAX_LENGTH_BYTES = 64;

	public const int GETFILEMETADATACOUNTOPTIONS_API_LATEST = 1;

	public const int GETFILEMETADATACOUNT_API_LATEST = 1;

	public const int QUERYFILELISTOPTIONS_API_LATEST = 2;

	public const int QUERYFILELIST_API_LATEST = 2;

	public const int QUERYFILEOPTIONS_API_LATEST = 1;

	public const int QUERYFILE_API_LATEST = 1;

	public const int READFILEOPTIONS_API_LATEST = 2;

	public const int READFILE_API_LATEST = 2;

	public const int TIME_UNDEFINED = -1;

	public const int WRITEFILEOPTIONS_API_LATEST = 2;

	public const int WRITEFILE_API_LATEST = 2;

	public PlayerDataStorageInterface()
	{
	}

	public PlayerDataStorageInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyFileMetadataAtIndex(ref CopyFileMetadataAtIndexOptions copyFileMetadataOptions, out FileMetadata? outMetadata)
	{
		CopyFileMetadataAtIndexOptionsInternal copyFileMetadataOptions2 = default(CopyFileMetadataAtIndexOptionsInternal);
		copyFileMetadataOptions2.Set(ref copyFileMetadataOptions);
		IntPtr outMetadata2 = IntPtr.Zero;
		Result result = Bindings.EOS_PlayerDataStorage_CopyFileMetadataAtIndex(base.InnerHandle, ref copyFileMetadataOptions2, out outMetadata2);
		Helper.Dispose(ref copyFileMetadataOptions2);
		Helper.Get<FileMetadataInternal, FileMetadata>(outMetadata2, out outMetadata);
		if (outMetadata2 != IntPtr.Zero)
		{
			Bindings.EOS_PlayerDataStorage_FileMetadata_Release(outMetadata2);
		}
		return result;
	}

	public Result CopyFileMetadataByFilename(ref CopyFileMetadataByFilenameOptions copyFileMetadataOptions, out FileMetadata? outMetadata)
	{
		CopyFileMetadataByFilenameOptionsInternal copyFileMetadataOptions2 = default(CopyFileMetadataByFilenameOptionsInternal);
		copyFileMetadataOptions2.Set(ref copyFileMetadataOptions);
		IntPtr outMetadata2 = IntPtr.Zero;
		Result result = Bindings.EOS_PlayerDataStorage_CopyFileMetadataByFilename(base.InnerHandle, ref copyFileMetadataOptions2, out outMetadata2);
		Helper.Dispose(ref copyFileMetadataOptions2);
		Helper.Get<FileMetadataInternal, FileMetadata>(outMetadata2, out outMetadata);
		if (outMetadata2 != IntPtr.Zero)
		{
			Bindings.EOS_PlayerDataStorage_FileMetadata_Release(outMetadata2);
		}
		return result;
	}

	public Result DeleteCache(ref DeleteCacheOptions options, object clientData, OnDeleteCacheCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		DeleteCacheOptionsInternal options2 = default(DeleteCacheOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Result result = Bindings.EOS_PlayerDataStorage_DeleteCache(base.InnerHandle, ref options2, clientDataPointer, OnDeleteCacheCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
		return result;
	}

	public void DeleteFile(ref DeleteFileOptions deleteOptions, object clientData, OnDeleteFileCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		DeleteFileOptionsInternal deleteOptions2 = default(DeleteFileOptionsInternal);
		deleteOptions2.Set(ref deleteOptions);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Bindings.EOS_PlayerDataStorage_DeleteFile(base.InnerHandle, ref deleteOptions2, clientDataPointer, OnDeleteFileCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref deleteOptions2);
	}

	public void DuplicateFile(ref DuplicateFileOptions duplicateOptions, object clientData, OnDuplicateFileCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		DuplicateFileOptionsInternal duplicateOptions2 = default(DuplicateFileOptionsInternal);
		duplicateOptions2.Set(ref duplicateOptions);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Bindings.EOS_PlayerDataStorage_DuplicateFile(base.InnerHandle, ref duplicateOptions2, clientDataPointer, OnDuplicateFileCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref duplicateOptions2);
	}

	public Result GetFileMetadataCount(ref GetFileMetadataCountOptions getFileMetadataCountOptions, out int outFileMetadataCount)
	{
		GetFileMetadataCountOptionsInternal getFileMetadataCountOptions2 = default(GetFileMetadataCountOptionsInternal);
		getFileMetadataCountOptions2.Set(ref getFileMetadataCountOptions);
		Result result = Bindings.EOS_PlayerDataStorage_GetFileMetadataCount(base.InnerHandle, ref getFileMetadataCountOptions2, out outFileMetadataCount);
		Helper.Dispose(ref getFileMetadataCountOptions2);
		return result;
	}

	public void QueryFile(ref QueryFileOptions queryFileOptions, object clientData, OnQueryFileCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		QueryFileOptionsInternal queryFileOptions2 = default(QueryFileOptionsInternal);
		queryFileOptions2.Set(ref queryFileOptions);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Bindings.EOS_PlayerDataStorage_QueryFile(base.InnerHandle, ref queryFileOptions2, clientDataPointer, OnQueryFileCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref queryFileOptions2);
	}

	public void QueryFileList(ref QueryFileListOptions queryFileListOptions, object clientData, OnQueryFileListCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		QueryFileListOptionsInternal queryFileListOptions2 = default(QueryFileListOptionsInternal);
		queryFileListOptions2.Set(ref queryFileListOptions);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Bindings.EOS_PlayerDataStorage_QueryFileList(base.InnerHandle, ref queryFileListOptions2, clientDataPointer, OnQueryFileListCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref queryFileListOptions2);
	}

	public PlayerDataStorageFileTransferRequest ReadFile(ref ReadFileOptions readOptions, object clientData, OnReadFileCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		ReadFileOptionsInternal readOptions2 = default(ReadFileOptionsInternal);
		readOptions2.Set(ref readOptions);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Helper.AddCallback(clientDataPointer, readOptions.ReadFileDataCallback);
		Helper.AddCallback(clientDataPointer, readOptions.FileTransferProgressCallback);
		IntPtr intPtr = Bindings.EOS_PlayerDataStorage_ReadFile(base.InnerHandle, ref readOptions2, clientDataPointer, OnReadFileCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref readOptions2);
		Helper.Get(intPtr, out PlayerDataStorageFileTransferRequest to);
		return to;
	}

	public PlayerDataStorageFileTransferRequest WriteFile(ref WriteFileOptions writeOptions, object clientData, OnWriteFileCompleteCallback completionCallback)
	{
		if (completionCallback == null)
		{
			throw new ArgumentNullException("completionCallback");
		}
		WriteFileOptionsInternal writeOptions2 = default(WriteFileOptionsInternal);
		writeOptions2.Set(ref writeOptions);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionCallback);
		Helper.AddCallback(clientDataPointer, writeOptions.WriteFileDataCallback);
		Helper.AddCallback(clientDataPointer, writeOptions.FileTransferProgressCallback);
		IntPtr intPtr = Bindings.EOS_PlayerDataStorage_WriteFile(base.InnerHandle, ref writeOptions2, clientDataPointer, OnWriteFileCompleteCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref writeOptions2);
		Helper.Get(intPtr, out PlayerDataStorageFileTransferRequest to);
		return to;
	}
}
