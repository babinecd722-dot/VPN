using System;

namespace Epic.OnlineServices.Ecom;

internal struct KeyImageInfoInternal : IGettable<KeyImageInfo>
{
	private int m_ApiVersion;

	private IntPtr m_Type;

	private IntPtr m_Url;

	private uint m_Width;

	private uint m_Height;

	public void Get(out KeyImageInfo other)
	{
		other = default(KeyImageInfo);
		Helper.Get(m_Type, out Utf8String to);
		other.Type = to;
		Helper.Get(m_Url, out Utf8String to2);
		other.Url = to2;
		other.Width = m_Width;
		other.Height = m_Height;
	}
}
