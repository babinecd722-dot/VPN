namespace Epic.OnlineServices.Version;

public sealed class VersionInterface
{
	public static readonly Utf8String COMPANY_NAME = "Epic Games, Inc.";

	public static readonly Utf8String COPYRIGHT_STRING = "Copyright Epic Games, Inc. All Rights Reserved.";

	public const int HOTFIX = 3;

	public const int MAJOR = 1;

	public const int MINOR = 19;

	public const int PATCH = 0;

	public static readonly Utf8String PRODUCT_IDENTIFIER = "Epic Online Services SDK";

	public static readonly Utf8String PRODUCT_NAME = "Epic Online Services SDK";

	public static Utf8String GetVersion()
	{
		Helper.Get(Bindings.EOS_GetVersion(), out Utf8String to);
		return to;
	}
}
