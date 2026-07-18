namespace Epic.OnlineServices.Metrics;

public struct BeginPlayerSessionOptionsAccountId
{
	private EpicAccountId m_Epic;

	private Utf8String m_External;

	private MetricsAccountIdType m_AccountIdType;

	public EpicAccountId Epic
	{
		get
		{
			if (m_AccountIdType == MetricsAccountIdType.Epic)
			{
				return m_Epic;
			}
			return null;
		}
		set
		{
			m_Epic = value;
			m_AccountIdType = MetricsAccountIdType.Epic;
		}
	}

	public Utf8String External
	{
		get
		{
			if (m_AccountIdType == MetricsAccountIdType.External)
			{
				return m_External;
			}
			return null;
		}
		set
		{
			m_External = value;
			m_AccountIdType = MetricsAccountIdType.External;
		}
	}

	public MetricsAccountIdType AccountIdType => m_AccountIdType;

	public static implicit operator BeginPlayerSessionOptionsAccountId(EpicAccountId value)
	{
		return new BeginPlayerSessionOptionsAccountId
		{
			Epic = value
		};
	}

	public static implicit operator BeginPlayerSessionOptionsAccountId(Utf8String value)
	{
		return new BeginPlayerSessionOptionsAccountId
		{
			External = value
		};
	}

	public static implicit operator BeginPlayerSessionOptionsAccountId(string value)
	{
		return new BeginPlayerSessionOptionsAccountId
		{
			External = value
		};
	}
}
