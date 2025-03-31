using System.Text.RegularExpressions;

namespace CustomFoodNamesMod.Utils
{
    /// <summary>
    /// String manipulation utilities
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Cleans up ingredient labels for better dish names
        /// </summary>
        public static string CleanIngredientLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "unknown ingredient";

            // Handle special case for twisted meat before anything else
            if (label.Contains("twisted meat") || label.Contains("Twisted meat") ||
                label.Contains("twisted flesh") || label.Contains("TwistedMeat"))
            {
                return "twisted meat"; // Always return consistent form for twisted meat
            }

            // Remove "raw" prefix
            string cleaned = Regex.Replace(label, @"^raw\s+", "", RegexOptions.IgnoreCase);

            // Some specific replacements
            cleaned = cleaned.Replace(" (unfert.)", "");
            cleaned = cleaned.Replace(" (fert.)", "");
            cleaned = cleaned.Replace(" meat", "");

            cleaned = cleaned.Trim();

            // Ensure first letter is lowercase for listing in text
            if (cleaned.Length > 0)
            {
                cleaned = char.ToLower(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned;
        }

        /// <summary>
        /// Gets a capitalized version of the ingredient label
        /// </summary>
        public static string GetCapitalizedLabel(string label)
        {
            // Special case for twisted meat
            if (label.Contains("twisted meat") || label.Contains("Twisted meat") ||
                label.Contains("twisted flesh") || label.Contains("TwistedMeat"))
            {
                return "Twisted Meat"; // Always return capitalized form
            }

            string cleaned = CleanIngredientLabel(label);

            // Capitalize first letter
            if (cleaned.Length > 0)
            {
                cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1);
            }

            return cleaned;
        }

        /// <summary>
        /// Capitalizes the first letter of a string
        /// </summary>
        public static string CapitalizeFirst(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return char.ToUpper(text[0]) + text.Substring(1);
        }
    }
}