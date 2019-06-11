﻿using System;
using System.Linq;

namespace InstaSharper.Classes.Android.DeviceInfo
{
    public class AndroidDeviceGenerator
    {
        private static readonly string[] DEVICES =
        {
            "24/7.0; 380dpi; 1080x1920; OnePlus; ONEPLUS A3010; OnePlus3T; qcom",
            "23/6.0.1; 640dpi; 1440x2392; LGE/lge; RS988; h1; h1",
            "24/7.0; 640dpi; 1440x2560; HUAWEI; LON-L29; HWLON; hi3660",
            "23/6.0.1; 640dpi; 1440x2560; ZTE; ZTE A2017U; ailsa_ii; qcom",
            "23/6.0.1; 640dpi; 1440x2560; samsung; SM-G935F; hero2lte; samsungexynos8890",
            "23/6.0.1; 640dpi; 1440x2560; samsung; SM-G930F; herolte; samsungexynos8890"
        };

        private static AndroidDevice BuildDeviceFromString(string str)
        {
            var device = new AndroidDevice();
            var components = str.Split(';');
            if (components.Length != 7) throw new ArgumentException("User agent string provided is not valid");
            device.UserAgentString = str;
            device.AndroidVersion = AndroidVersion.FromString(components[0].Split('/')[1]);
            device.Dpi = int.Parse(components[1].Remove(components[1].Length - 3));
            var resolutionValues = components[2].Split('x');
            device.ScreenResolution.Width = int.Parse(resolutionValues[0]);
            device.ScreenResolution.Height = int.Parse(resolutionValues[1]);
            device.HardwareManufacturer = components[3].Split('/')[0];
            device.HardwareModel = components[4];
            device.DeviceName = components[5];
            device.Cpu = components[6];
            device.DeviceId = ApiRequestMessage.GenerateDeviceIdFromGuid(device.Uuid);
            return device;
        }

        public static AndroidDevice GetRandomAndroidDevice()
        {
            var random = new Random(DateTime.Now.Millisecond);
            var randomDeviceIndex = random.Next(0, DEVICES.Length);
            return BuildDeviceFromString(DEVICES[randomDeviceIndex]);
        }

        public static AndroidDevice GetById(string deviceId)
        {
            var device = GetRandomAndroidDevice();
            device.DeviceId = deviceId;
            return device;
        }
    }
}