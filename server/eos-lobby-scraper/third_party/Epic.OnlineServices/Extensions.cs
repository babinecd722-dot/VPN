using System;

namespace Epic.OnlineServices;

public static class Extensions
{
	public static bool IsOperationComplete(this Result result)
	{
		return Common.IsOperationComplete(result);
	}

	public static Utf8String ToHexString(this byte[] byteArray)
	{
		return Common.ToString(new ArraySegment<byte>(byteArray));
	}

	public static Utf8String ToHexString(this ArraySegment<byte> arraySegment)
	{
		return Common.ToString(arraySegment);
	}
}
