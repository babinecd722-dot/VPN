using System;

namespace Epic.OnlineServices.TitleStorage;

internal struct QueryFileListOptionsInternal : ISettable<QueryFileListOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_ListOfTags;

	private uint m_ListOfTagsCount;

	public void Set(ref QueryFileListOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set(other.ListOfTags, ref m_ListOfTags, out m_ListOfTagsCount, isArrayItemAllocated: true);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_ListOfTags);
	}
}
