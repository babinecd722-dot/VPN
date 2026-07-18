using System;

namespace Epic.OnlineServices.Lobby;

internal struct AttributeDataInternal : IGettable<AttributeData>, ISettable<AttributeData>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	private AttributeDataValueInternal m_Value;

	private AttributeType m_ValueType;

	public void Get(out AttributeData other)
	{
		other = default(AttributeData);
		Helper.Get(m_Key, out Utf8String to);
		other.Key = to;
		m_Value.Get(out var other2, m_ValueType, null);
		other.Value = other2;
	}

	public void Set(ref AttributeData other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Key, ref m_Key);
		Helper.Set<AttributeDataValue, AttributeDataValueInternal>(other.Value, ref m_Value);
		m_ValueType = other.Value.ValueType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Key);
		Helper.Dispose(ref m_Value);
	}
}
