using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;
using Android.Util;
using System.Threading.Tasks;
using System.Globalization;

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
            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), GetIso8601WeekOfYear(_dateTime).ToString());
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
            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), GetIso8601WeekOfYear(_dateTime).ToString());
            var filePath = Path.Combine(directoryPath, _date + ".json");
            var success = false;

            await Task.Factory.StartNew(() =>
            {
                if (!File.Exists(filePath))
                {
                    success = false;
                }
            });

            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(content);
                success = true;
            }

            return success;
        }
    }
}