using Dapper;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Net5SingleFile
{
    class Program
    {
        //透過程式設定 NLog 時，要留意 statci logger 取得物件時機
        //要排在 SetupNLog() 之後
        static ILogger logger = null;

        static void SetupNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();
            //偵測程式是不是被放在桌面
            //注意：Assembly.Location 不能用
            var onDesktop =
                AppContext.BaseDirectory ==
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var logFile = new NLog.Targets.FileTarget("f")
            {
                FileName =
                    (onDesktop ? Path.GetTempPath() : "${basedir}") +
                    "/MyToolLogs/${shortdate}.log"
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logFile);
            LogManager.Configuration = config;
            logger = NLog.LogManager.GetLogger("debug");
        }

        static string dbFilePath = Path.Combine(AppContext.BaseDirectory, "data.db");
        static string cnStr = $"data source={dbFilePath}";
        static SqliteConnection GetConnection()
        {
            if (!File.Exists(dbFilePath))
            {
                using (var cnInit = new SqliteConnection(cnStr))
                {
                    cnInit.Execute(@"
CREATE TABLE JsonData (
    Id INTEGER NOT NULL,
    Json NVARCHAR(255) NOT NULL,
    CONSTRAINT JsonData_PK PRIMARY KEY (Id)
)");
                }
            }
            return new SqliteConnection(cnStr); ;
        }
        static void Main(string[] args)
        {
            SetupNLog();
            logger.Debug("Hello, World!");
            using (var cn = GetConnection())
            {
                cn.Execute("DELETE FROM JsonData WHERE Id = 1");
                cn.Execute("INSERT INTO JsonData VALUES(1, @json)", new
                {
                    json = JsonConvert.SerializeObject(new
                    {
                        Time = DateTime.Now
                    })
                });
                var chk = cn.Query<string>("SELECT json FROM JsonData WHERE Id = 1").First();
                Console.WriteLine(chk);
            }
            Console.WriteLine("Done! press enter to quite");
            Console.ReadLine();
        }
    }
}
