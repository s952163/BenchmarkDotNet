﻿using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Environments
{
    public class GcResolver : Resolver
    {
        public static readonly IResolver Instance = new GcResolver();

        private GcResolver()
        {
            Register(GcMode.ServerCharacteristic, () => HostEnvironmentInfo.GetCurrent().IsServerGC);
            Register(GcMode.ConcurrentCharacteristic, () => HostEnvironmentInfo.GetCurrent().IsConcurrentGC);
            Register(GcMode.CpuGroupsCharacteristic, () => false);
            Register(GcMode.ForceCharacteristic, () => true);
            Register(GcMode.AllowVeryLargeObjectsCharacteristic, () => false);
        }
    }
}