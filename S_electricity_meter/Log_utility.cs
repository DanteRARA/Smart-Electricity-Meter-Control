using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Org.BouncyCastle.Asn1.Cms;

namespace Log_Utility
{
    using System.IO;

    public static class Logger
    {
        private static readonly object _lock = new object();  // 確保多執行緒安全
        private static string fileDate = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        private static string _logFilePath = $"../../../Logs/ParkingLot_{DateTime.Now:yyyy-MM-dd}_log.txt";  // 預設 Log 檔案名稱
        private static bool _logToFile = true;  // 是否寫入檔案
        private static bool _logToConsole = true;  // 是否輸出到 Console

        /// <summary>
        /// 設定 Log 儲存位置
        /// </summary>
        public static void Configure(string logFilePath, bool logToFile = true, bool logToConsole = true)
        {
            _logFilePath = logFilePath;
            _logToFile = logToFile;
            _logToConsole = logToConsole;
        }

        /// <summary>
        /// 記錄 Info 訊息
        /// </summary>
        public static void Info(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// 記錄 Warning 訊息
        /// </summary>
        public static void Warning(string message)
        {
            Log("WARNING", message);
        }

        /// <summary>
        /// 記錄 Error 訊息
        /// </summary>
        public static void Error(string message)
        {
            Log("ERROR", message);
        }

        /// <summary>
        /// 通用 Log 方法
        /// </summary>
        private static void Log(string level, string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

            lock (_lock) // 確保多執行緒寫入安全
            {
                if (_logToConsole)
                {
                    Console.WriteLine(logEntry);
                }

                if (_logToFile)
                {
                    try
                    {
                        string? dir = Path.GetDirectoryName(_logFilePath);
                        if(!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)){
                            Directory.CreateDirectory(dir);
                        }                   
                        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"寫入 Log 檔案失敗: {ex.Message}");
                    }
                }
            }
        }
    }
}
