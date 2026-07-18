namespace Epic.OnlineServices.Lobby;

public struct AttributeDataValue
{
	private long? m_AsInt64;

	private double? m_AsDouble;

	private bool? m_AsBool;

	private Utf8String m_AsUtf8;

	private AttributeType m_ValueType;

	public long? AsInt64
	{
		get
		{
			if (m_ValueType == AttributeType.Int64)
			{
				return m_AsInt64;
			}
			return null;
		}
		set
		{
			m_AsInt64 = value;
			m_ValueType = AttributeType.Int64;
		}
	}

	public double? AsDouble
	{
		get
		{
			if (m_ValueType == AttributeType.Double)
			{
				return m_AsDouble;
			}
			return null;
		}
		set
		{
			m_AsDouble = value;
			m_ValueType = AttributeType.Double;
		}
	}

	public bool? AsBool
	{
		get
		{
			if (m_ValueType == AttributeType.Boolean)
			{
				return m_AsBool;
			}
			return null;
		}
		set
		{
			m_AsBool = value;
			m_ValueType = AttributeType.Boolean;
		}
	}

	public Utf8String AsUtf8
	{
		get
		{
			if (m_ValueType == AttributeType.String)
			{
				return m_AsUtf8;
			}
			return null;
		}
		set
		{
			m_AsUtf8 = value;
			m_ValueType = AttributeType.String;
		}
	}

	public AttributeType ValueType => m_ValueType;

	public static implicit operator AttributeDataValue(long? value)
	{
		return new AttributeDataValue
		{
			AsInt64 = value
		};
	}

	public static implicit operator AttributeDataValue(double? value)
	{
		return new AttributeDataValue
		{
			AsDouble = value
		};
	}

	public static implicit operator AttributeDataValue(bool? value)
	{
		return new AttributeDataValue
		{
			AsBool = value
		};
	}

	public static implicit operator AttributeDataValue(Utf8String value)
	{
		return new AttributeDataValue
		{
			AsUtf8 = value
		};
	}

	public static implicit operator AttributeDataValue(string value)
	{
		return new AttributeDataValue
		{
			AsUtf8 = value
		};
	}
}
