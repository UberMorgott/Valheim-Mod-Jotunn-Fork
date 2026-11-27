using System;
using System.Collections.Generic;

namespace Jotunn.Utils
{
    internal static class EnumNameCache
    {
        public static string GetName<T>(T value) where T : struct, Enum
        {
            return Cache<T>.TryGetName(value, out var name) ? name : string.Empty;
        }

        public static string GetName<T>(T value, int rawValue) where T : struct, Enum
        {
            return Cache<T>.TryGetName(value, out var name) ? name : rawValue.ToString();
        }

        public static string GetNameOrNull<T>(T value) where T : struct, Enum
        {
            return Cache<T>.TryGetName(value, out var name) ? name : null;
        }

        public static string GetFlagsName<T>(T value, int rawValue) where T : struct, Enum
        {
            if (Cache<T>.TryGetName(value, out var exact))
            {
                return exact;
            }

            return Cache<T>.FormatFlags(rawValue);
        }

        private static class Cache<T> where T : struct, Enum
        {
            private static readonly Dictionary<T, string> Map;
            private static readonly FlagEntry[] Flags;

            private struct FlagEntry
            {
                public int Bits;
                public string Name;
            }

            static Cache()
            {
                var values = (T[])Enum.GetValues(typeof(T));
                var names = Enum.GetNames(typeof(T));
                var map = new Dictionary<T, string>(values.Length);
                var flagList = new List<FlagEntry>(values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    if (!map.ContainsKey(values[i]))
                    {
                        map.Add(values[i], names[i]);
                    }
                    int bits = Convert.ToInt32(values.GetValue(i), System.Globalization.CultureInfo.InvariantCulture);
                    if (bits != 0 && (bits & (bits - 1)) == 0)
                    {
                        flagList.Add(new FlagEntry { Bits = bits, Name = names[i] });
                    }
                }
                Map = map;
                Flags = flagList.ToArray();
            }

            public static bool TryGetName(T value, out string name)
            {
                return Map.TryGetValue(value, out name);
            }

            public static string FormatFlags(int rawValue)
            {
                if (rawValue == 0)
                {
                    return "0";
                }

                int remaining = rawValue;
                List<string> matched = null;
                for (int i = 0; i < Flags.Length; i++)
                {
                    if ((remaining & Flags[i].Bits) == Flags[i].Bits)
                    {
                        if (matched == null)
                        {
                            matched = new List<string>();
                        }
                        matched.Add(Flags[i].Name);
                        remaining &= ~Flags[i].Bits;
                    }
                }

                if (remaining == 0 && matched != null && matched.Count > 0)
                {
                    return string.Join(", ", matched);
                }

                return rawValue.ToString();
            }
        }
    }
}
