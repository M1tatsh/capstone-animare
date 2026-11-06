using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Data.Playtest
{
    /// <summary>
    /// Adds rows to CSV files in the PlaytestLogs folder (AppData/LocalLow/DeathArtStudios/Animare)
    /// </summary>
    public static class CSVDataLogger
    {
        public static string FolderPath => Path.Combine(Application.persistentDataPath, "PlaytestLogs");


        public static void AppendRow(string fileName, string[] header, object[] values)
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                string filePath = Path.Combine(FolderPath, fileName);

                StringBuilder text = new StringBuilder();

                if (!File.Exists(filePath))
                    text.AppendLine(JoinRow(header));

                text.AppendLine(JoinRow(values));

                File.AppendAllText(filePath, text.ToString());
                
            }
            catch (Exception ex) {
                Debug.LogWarning($"CsvFile: Could not write to {fileName}.\n{ex.Message}");
            }
        }

        private static string JoinRow(object[] values)
        {
            string[] cells = new string[values.Length];

            for (int i = 0; i < values.Length; i++)
                cells[i] = ToCell(values[i]);

            return string.Join(",", cells);
        }


        private static string ToCell(object value)
        {
            if (value == null)
                return "";


            string text;

            //specifying invariant culture here to ensure numbers use dots.
            if (value is float floatValue)
                text = floatValue.ToString("0.###", CultureInfo.InvariantCulture);
            else if (value is IFormattable formattable)
                text = formattable.ToString(null, CultureInfo.InvariantCulture);
            else
                text = value.ToString();


            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n"))
                text = "\"" + text.Replace("\"", "\"\"") + "\"";

            return text;
        }
    }
}
