using System;
using System.Data.Common;

namespace SysPuntoVenta.Backend.Data
{
    public static class DataReaderExtensions
    {
        public static string SafeGetString(this DbDataReader reader, string column, string defaultValue = "")
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return defaultValue;
                return reader.GetValue(ordinal)?.ToString() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static int SafeGetInt32(this DbDataReader reader, string column, int defaultValue = 0)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return defaultValue;
                return Convert.ToInt32(reader.GetValue(ordinal));
            }
            catch
            {
                return defaultValue;
            }
        }

        public static decimal SafeGetDecimal(this DbDataReader reader, string column, decimal defaultValue = 0m)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return defaultValue;
                return Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch
            {
                return defaultValue;
            }
        }

        public static DateTime? SafeGetDateTime(this DbDataReader reader, string column)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return null;
                return reader.GetDateTime(ordinal);
            }
            catch
            {
                return null;
            }
        }
    }
}
