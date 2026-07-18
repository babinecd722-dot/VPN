using System;
using System.Runtime.InteropServices;

namespace Epic.OnlineServices.Sessions;

[StructLayout(LayoutKind.Explicit)]
internal struct AttributeDataValueInternal : IGettable<AttributeDataValue, AttributeType>, ISettable<AttributeDataValue>, IDisposable
{
	[FieldOffset(0)]
	private long m_AsInt64;

	[FieldOffset(0)]
	private double m_AsDouble;

	[FieldOffset(0)]
	private int m_AsBool;

	[FieldOffset(0)]
	private IntPtr m_AsUtf8;

	public void Get(out AttributeDataValue other, AttributeType enumValue, int? arrayLength)
	{
		other = default(AttributeDataValue);
		if (enumValue == AttributeType.Int64)
		{
			Helper.Get(m_AsInt64, out long? to);
			other.AsInt64 = to;
		}
		if (enumValue == AttributeType.Double)
		{
			Helper.Get(m_AsDouble, out var to2);
			other.AsDouble = to2;
		}
		if (enumValue == AttributeType.Boolean)
		{
			Helper.Get(m_AsBool, out bool? to3);
			other.AsBool = to3;
		}
		if (enumValue == AttributeType.String)
		{
			Helper.Get(m_AsUtf8, out Utf8String to4);
			other.AsUtf8 = to4;
		}
	}

	public void Set(ref AttributeDataValue other)
	{
		Dispose();
		if (other.ValueType == AttributeType.Int64)
		{
			Helper.Set<long>(other.AsInt64, ref m_AsInt64);
		}
		if (other.ValueType == AttributeType.Double)
		{
			Helper.Set<double>(other.AsDouble, ref m_AsDouble);
		}
		if (other.ValueType == AttributeType.Boolean)
		{
			Helper.Set(other.AsBool, ref m_AsBool);
		}
		if (other.ValueType == AttributeType.String)
		{
			Helper.Set(other.AsUtf8, ref m_AsUtf8);
		}
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AsUtf8);
	}
}
