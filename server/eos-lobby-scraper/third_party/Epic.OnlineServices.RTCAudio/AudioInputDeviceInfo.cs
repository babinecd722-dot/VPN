namespace Epic.OnlineServices.RTCAudio;

public struct AudioInputDeviceInfo
{
	public bool DefaultDevice { get; set; }

	public Utf8String DeviceId { get; set; }

	public Utf8String DeviceName { get; set; }
}
