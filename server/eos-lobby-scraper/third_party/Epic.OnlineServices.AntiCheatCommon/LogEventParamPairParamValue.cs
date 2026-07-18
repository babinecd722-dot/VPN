using System;

namespace Epic.OnlineServices.AntiCheatCommon;

public struct LogEventParamPairParamValue
{
	private IntPtr? m_ClientHandle;

	private Utf8String m_String;

	private uint? m_UInt32;

	private int? m_Int32;

	private ulong? m_UInt64;

	private long? m_Int64;

	private Vec3f? m_Vec3f;

	private Quat? m_Quat;

	private float? m_Float;

	private AntiCheatCommonEventParamType m_ParamValueType;

	public IntPtr? ClientHandle
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.ClientHandle)
			{
				return m_ClientHandle;
			}
			return null;
		}
		set
		{
			m_ClientHandle = value;
			m_ParamValueType = AntiCheatCommonEventParamType.ClientHandle;
		}
	}

	public Utf8String String
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.String)
			{
				return m_String;
			}
			return null;
		}
		set
		{
			m_String = value;
			m_ParamValueType = AntiCheatCommonEventParamType.String;
		}
	}

	public uint? UInt32
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.UInt32)
			{
				return m_UInt32;
			}
			return null;
		}
		set
		{
			m_UInt32 = value;
			m_ParamValueType = AntiCheatCommonEventParamType.UInt32;
		}
	}

	public int? Int32
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.Int32)
			{
				return m_Int32;
			}
			return null;
		}
		set
		{
			m_Int32 = value;
			m_ParamValueType = AntiCheatCommonEventParamType.Int32;
		}
	}

	public ulong? UInt64
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.UInt64)
			{
				return m_UInt64;
			}
			return null;
		}
		set
		{
			m_UInt64 = value;
			m_ParamValueType = AntiCheatCommonEventParamType.UInt64;
		}
	}

	public long? Int64
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.Int64)
			{
				return m_Int64;
			}
			return null;
		}
		set
		{
			m_Int64 = value;
			m_ParamValueType = AntiCheatCommonEventParamType.Int64;
		}
	}

	public Vec3f? Vec3f
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.Vector3f)
			{
				return m_Vec3f;
			}
			return null;
		}
		set
		{
			m_Vec3f = value;
			m_ParamValueType = AntiCheatCommonEventParamType.Vector3f;
		}
	}

	public Quat? Quat
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.Quat)
			{
				return m_Quat;
			}
			return null;
		}
		set
		{
			m_Quat = value;
			m_ParamValueType = AntiCheatCommonEventParamType.Quat;
		}
	}

	public float? Float
	{
		get
		{
			if (m_ParamValueType == AntiCheatCommonEventParamType.Float)
			{
				return m_Float;
			}
			return null;
		}
		set
		{
			m_Float = value;
			m_ParamValueType = AntiCheatCommonEventParamType.Float;
		}
	}

	public AntiCheatCommonEventParamType ParamValueType => m_ParamValueType;

	public static implicit operator LogEventParamPairParamValue(IntPtr? value)
	{
		return new LogEventParamPairParamValue
		{
			ClientHandle = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(Utf8String value)
	{
		return new LogEventParamPairParamValue
		{
			String = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(string value)
	{
		return new LogEventParamPairParamValue
		{
			String = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(uint? value)
	{
		return new LogEventParamPairParamValue
		{
			UInt32 = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(int? value)
	{
		return new LogEventParamPairParamValue
		{
			Int32 = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(ulong? value)
	{
		return new LogEventParamPairParamValue
		{
			UInt64 = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(long? value)
	{
		return new LogEventParamPairParamValue
		{
			Int64 = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(Vec3f? value)
	{
		return new LogEventParamPairParamValue
		{
			Vec3f = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(Quat? value)
	{
		return new LogEventParamPairParamValue
		{
			Quat = value
		};
	}

	public static implicit operator LogEventParamPairParamValue(float? value)
	{
		return new LogEventParamPairParamValue
		{
			Float = value
		};
	}
}
