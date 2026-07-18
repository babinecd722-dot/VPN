namespace Epic.OnlineServices.Presence;

public struct PresenceModificationSetTemplateDataOptionsValue
{
	private int? m_AsInt32;

	private Utf8String m_AsStringId;

	private PresenceModificationTemplateType m_ValueType;

	public int? AsInt32
	{
		get
		{
			if (m_ValueType == PresenceModificationTemplateType.Int)
			{
				return m_AsInt32;
			}
			return null;
		}
		set
		{
			m_AsInt32 = value;
			m_ValueType = PresenceModificationTemplateType.Int;
		}
	}

	public Utf8String AsStringId
	{
		get
		{
			if (m_ValueType == PresenceModificationTemplateType.String)
			{
				return m_AsStringId;
			}
			return null;
		}
		set
		{
			m_AsStringId = value;
			m_ValueType = PresenceModificationTemplateType.String;
		}
	}

	public PresenceModificationTemplateType ValueType => m_ValueType;

	public static implicit operator PresenceModificationSetTemplateDataOptionsValue(int? value)
	{
		return new PresenceModificationSetTemplateDataOptionsValue
		{
			AsInt32 = value
		};
	}

	public static implicit operator PresenceModificationSetTemplateDataOptionsValue(Utf8String value)
	{
		return new PresenceModificationSetTemplateDataOptionsValue
		{
			AsStringId = value
		};
	}

	public static implicit operator PresenceModificationSetTemplateDataOptionsValue(string value)
	{
		return new PresenceModificationSetTemplateDataOptionsValue
		{
			AsStringId = value
		};
	}
}
