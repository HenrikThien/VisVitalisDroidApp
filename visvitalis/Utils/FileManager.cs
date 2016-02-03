using System;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using Android.Util;
using System.Text;
using System.Collections.Generic;

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

        public async Task<string> CreateFileAsync()
        {
            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), "temp");
            var filePath = Path.Combine(directoryPath, _date + ".json");

            var maskFilePath = Path.Combine(FolderPath, AppConstants.DataFolder, "futuremasks", "mask.json");
            var maskFileContent = "";

            using (var reader = new StreamReader(maskFilePath))
            {
                maskFileContent = await reader.ReadToEndAsync();
                reader.Close();
            }

            using (var fs = File.Create(filePath))
            {
                var buffer = Encoding.UTF8.GetBytes(maskFileContent);
                await fs.WriteAsync(buffer, 0, buffer.Length);
                await fs.FlushAsync();
                fs.Close();
            }

            return maskFileContent;
        }

        public async Task<string> LoadFileAsync()
        {
            var weekId = GetIso8601WeekOfYear(_dateTime);
            var newWeekId = (weekId < 10) ? "0" + weekId.ToString() : weekId.ToString();

            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), "temp");
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
        public string LoadFile(bool oldFile = false)
        { 
            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), "temp");
            if (oldFile)
                directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), "temp", "old.data");

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
            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, _dateTime.Year.ToString(), "temp");
            var filePath = Path.Combine(directoryPath, _date + ".json");
            var success = false;

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

        public async Task MoveOldFilesAsync(string year)
        {
            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, year, "temp");
            var newDirectoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, year, "temp", "old.data");

            var tempList = new List<string>();
            await Task.Factory.StartNew(() =>
            {
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    tempList.Add(file);
                }
            });

            await Task.Factory.StartNew(() =>
            {
                foreach (var file in tempList)
                {
                    var fileName = Path.GetFileName(file);
                    if (File.Exists(Path.Combine(newDirectoryPath, fileName)))
                    {
                        File.Delete(Path.Combine(newDirectoryPath, fileName));
                    }
                    File.Move(file, Path.Combine(newDirectoryPath, fileName));
                }
            });

            tempList.Clear();
        }

        public async Task<List<string>> GetFileContentFromTempAsync(string year)
        {
            var resultList = new List<string>();
            var directoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, year, "temp");
            var newDirectoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, year, "temp", "old.data");

            var tempList = new List<string>();
            await Task.Factory.StartNew(() =>
            {
                foreach (var file in Directory.GetFiles(directoryPath))
                {
                    tempList.Add(file);
                }
            });

            foreach (var file in tempList)
            {
                using (var streamReader = new StreamReader(file))
                {
                    resultList.Add(await streamReader.ReadToEndAsync());
                    streamReader.Close();
                }
            }

            tempList.Clear();

            return resultList;
        }

        public async Task<string> SearchOldFileContent(string year)
        {
            var content = "[]";
            var newDirectoryPath = Path.Combine(FolderPath, AppConstants.DataFolder, year, "temp", "old.data");

            foreach (var file in Directory.GetFiles(newDirectoryPath))
            {
                var name = Path.GetFileName(file);

                if (name.Contains(_date))
                {
                    using (var streamReader = new StreamReader(file))
                    {
                        content = await streamReader.ReadToEndAsync();
                    }
                }
            }

            return content;
        }
    }
}