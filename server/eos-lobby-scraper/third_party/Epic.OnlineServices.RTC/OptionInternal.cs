using System;

namespace Epic.OnlineServices.RTC;

internal struct OptionInternal : IGettable<Option>
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	private IntPtr m_Value;

	public void Get(out Option other)
	{
		other = default(Option);
		Helper.Get(m_Key, out Utf8String to);
		other.Key = to;
		Helper.Get(m_Value, out Utf8String to2);
		other.Value = to2;
	}
}
