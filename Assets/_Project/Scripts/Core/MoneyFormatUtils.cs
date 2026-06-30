using UnityEngine;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Stateless helpers that turn money values into compact display strings, so HUDs hold no
    /// formatting logic. Values under a thousand show as whole dollars (e.g. "$125", no cents); larger
    /// values are floored to one decimal and abbreviated with K/M/B/T (e.g. "$1.5K", "$999.9K",
    /// "$3.4M"). Flooring (not rounding) means the balance is never overstated and never carries to
    /// the next tier ("$999.9K", not "$1000K"). Negative balances (debt) keep their sign ("-$1.5K").
    /// </summary>
    public static class MoneyFormatUtils
    {
        private const float THOUSAND = 1000.0f;
        private const float MILLION = 1000000.0f;
        private const float BILLION = 1000000000.0f;
        private const float TRILLION = 1000000000000.0f;
        private const float ONE_DECIMAL_SCALE = 10.0f;

        /// <summary>
        /// Formats a money amount as a compact currency string: whole dollars under $1,000, and floored
        /// to one decimal + abbreviated above — e.g. 1599 → "$1.5K", 999950 → "$999.9K".
        /// </summary>
        public static string Format(float amount)
        {
            string sign = amount < 0.0f ? "-" : "";
            float magnitude = Mathf.Abs(amount);
            string result;

            if (magnitude >= TRILLION)
            {
                result = FormatTier(sign, magnitude, TRILLION, "T");
            }
            else if (magnitude >= BILLION)
            {
                result = FormatTier(sign, magnitude, BILLION, "B");
            }
            else if (magnitude >= MILLION)
            {
                result = FormatTier(sign, magnitude, MILLION, "M");
            }
            else if (magnitude >= THOUSAND)
            {
                result = FormatTier(sign, magnitude, THOUSAND, "K");
            }
            else
            {
                result = $"{sign}${Mathf.FloorToInt(magnitude)}";
            }

            return result;
        }

        // Scales the magnitude into a tier, floors it to a single decimal (so it never rounds up or
        // carries into the next tier), and tags on the suffix.
        private static string FormatTier(string sign, float magnitude, float tierValue, string suffix)
        {
            float scaled = magnitude / tierValue;
            float flooredOneDecimal = Mathf.Floor(scaled * ONE_DECIMAL_SCALE) / ONE_DECIMAL_SCALE;

            return $"{sign}${flooredOneDecimal:0.0}{suffix}";
        }
    }
}
