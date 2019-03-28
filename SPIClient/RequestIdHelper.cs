using System.Runtime.InteropServices;

﻿namespace SPIClient
{
    /// <summary>
    /// These attributes work for COM interop.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public static class RequestIdHelper
    {
        private static int _counter = 1;

        public static string Id(string prefix)
        {
            return prefix + _counter++;
        }

    }
}