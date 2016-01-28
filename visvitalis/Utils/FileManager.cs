using System;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using Android.Util;

namespace visvitalis.Utils
{
    public sealed class FileManager : IDisposable
    {
        private readonly string FolderPath = Android.OS.Environment.ExternalStorageDirectory.Path;
        private DateTime _dateTime;
        private string _date;

        public FileManager(string date, DateTime dateTime)
        {
            _dateTime = dateTime;
            _date = date;
        }

        public async Task<string> LoadFileAsync()
        {
            var weekId = GetIso8601WeekOfYear(_dateTime);
            var newWeekId = (weekId < 10) ? "0" + weekId.ToString() : weekId.ToString();

            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), newWeekId);
            var filePath = Path.Combine(directoryPath, _date + ".json");

            var fileContent = "";

            await Task.Factory.StartNew(() =>
            {
                if (!File.Exists(filePath))
                {
                    fileContent = "undefined";
                }
            });

            if (fileContent != "undefined")
            {
                using (var reader = new StreamReader(filePath))
                {
                    fileContent = await reader.ReadToEndAsync();
                }
            }
            else
            {
                fileContent = "[]";
            }

            return fileContent;
        }
        public string LoadFile()
        {
            var weekId = GetIso8601WeekOfYear(_dateTime);
            var newWeekId = (weekId < 10) ? "0" + weekId.ToString() : weekId.ToString();

            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), newWeekId);
            var filePath = Path.Combine(directoryPath, _date + ".json");

            var fileContent = "";

            if (!File.Exists(filePath))
            {
                fileContent = "undefined";
            }

            if (fileContent != "undefined")
            {
                using (var reader = new StreamReader(filePath))
                {
                    fileContent = reader.ReadToEnd();
                }
            }
            else
            {
                fileContent = "[]";
            }

            return fileContent;
        }

        public int GetIso8601WeekOfYear(DateTime time)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public void Dispose()
        {
            _date = null;
        }

        public async Task<bool> SaveJsonContentAsync(string content)
        {
            var weekId = GetIso8601WeekOfYear(_dateTime);
            var newWeekId = (weekId < 10) ? "0" + weekId.ToString() : weekId.ToString();

            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), newWeekId);
            var filePath = Path.Combine(directoryPath, _date + ".json");
            var success = false;

            Log.Debug("debug", "Path to file: => " + filePath);

            if (await Task.Factory.StartNew(() => File.Exists(filePath)))
            {
                using (var writer = new StreamWriter(filePath))
                {
                    await writer.WriteAsync(content);
                    success = true;
                    await writer.FlushAsync();
                }
            }

            return success;
        }
    }
}