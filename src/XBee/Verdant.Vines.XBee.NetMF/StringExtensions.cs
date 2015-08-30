using System;
using Microsoft.SPOT;

namespace Verdant.Vines.XBee
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string value)
        {
            return value == null || value.Length == 0;
        }
    }
}
