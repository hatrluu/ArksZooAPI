using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Timers;
using System.Configuration;
using System.Web.Http.Cors;

namespace ArksZooAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BackupController : ControllerBase
    {
        private static string gamePath = ConfigurationManager.AppSettings["GamePath"];
        private static string currentServerPath = gamePath + "\\Saved";
        private static string backupServerPath = ConfigurationManager.AppSettings["BackupPath"];
        private static int numberOfSavesToKeep = 3;
        private static Timer saveTimer;
        private static Boolean backupStatus = false;


        //GET backup/latestbackup
        [Route("latestbackup")]
        [HttpGet]
        public string LatestBackup()
        {
            //Need to catch empty folder
            string latestBackup = "";
            DirectoryInfo[] di = new DirectoryInfo(backupServerPath).GetDirectories();
            var latestFile = di.OrderByDescending(x => x.LastWriteTime).First();
            latestBackup = latestFile.Name;
            return latestBackup.Remove(0,7);
        }

        //GET backup/backupstatus
        [Route("backupstatus")]
        [HttpGet]
        public Boolean BackupStatus()
        {
            return backupStatus ? true : false;
        }

        //GET Backup/settings
        [Route("settings")]
        [HttpGet]
        public List<string> GetSettings()
        {
            var appSettings = ConfigurationManager.AppSettings;
            List<string> settings = new List<string>();
            foreach( string key in appSettings.AllKeys)
            {
                settings.Add(String.Format("{0}: {1}", key, appSettings[key].ToString()));
            }
            return settings;
        }

        //GET Backup/startbackup
        [Route("startbackup")]
        [HttpGet]
        public string StartBackup()
        {
            int backupInterval = Int32.Parse(ConfigurationManager.AppSettings["BackupInterval"]);
            int hoursSave = Int32.Parse(ConfigurationManager.AppSettings["HoursSave"]);

            numberOfSavesToKeep = hoursSave * 60 / backupInterval;

            TimeChecker(backupInterval);
            backupStatus = true;
            return string.Format("Backup started at {0:hh:mm:ss.fff}", DateTime.Now);
            /*
            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:hh:mm:ss.fff}", DateTime.Now);
            Console.ReadLine();
            saveTimer.Stop();
            saveTimer.Dispose();
            */
        }

        //GET Backup/stopbackup
        [Route("stopbackup")]
        [HttpGet]
        public string StopBackup()
        {
            saveTimer.Stop();
            saveTimer.Dispose();
            backupStatus = false;
            return string.Format("Backup ended at {0:hh:mm:ss.fff}", DateTime.Now);
        }

        [Route("testbackup")]
        [HttpGet]
        public string TestBackup()
        {
            //Update path
            backupServerPath = ConfigurationManager.AppSettings["BackupPath"];

            string current = currentServerPath;
            string backup = backupServerPath+ "\\";
            DateTime localDateTime = DateTime.Now;
            string backupFolder = "backup-" + localDateTime.ToString("hhmmtt-MMdd");
            string newBackupLocation = backup + backupFolder;
            Directory.CreateDirectory(newBackupLocation);
            
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(current, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(current, newBackupLocation));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(current, "*.*", SearchOption.AllDirectories))
                System.IO.File.Copy(newPath, newPath.Replace(current, newBackupLocation), true);

            return string.Format("Backup Completed at {0}", backupFolder);
        }
        private static void TimeChecker(int minute)
        {
            // Create a timer 
            saveTimer = new System.Timers.Timer(1000 * 60 * minute);
            // Hook up the Elapsed event for the timer. 
            saveTimer.Elapsed += BackupSaved;
            saveTimer.AutoReset = true;
            saveTimer.Enabled = true;
        }
        private static void BackupSaved(Object source, ElapsedEventArgs e)
        {
            //Update path
            backupServerPath = ConfigurationManager.AppSettings["BackupPath"];

            string current = currentServerPath;
            string backup = backupServerPath+"\\";
            DateTime localDateTime = DateTime.Now;
            string backupFolder = "backup-" + localDateTime.ToString("hhmmtt-MMdd");
            string newBackupLocation = backup + backupFolder;
            Directory.CreateDirectory(newBackupLocation);

            try
            {
                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(current, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(current, newBackupLocation));

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(current, "*.*", SearchOption.AllDirectories))
                    System.IO.File.Copy(newPath, newPath.Replace(current, newBackupLocation), true);

                Console.WriteLine("Backup Completed at {0}", backupFolder);
            }
            catch (IOException err)
            {
                Console.WriteLine(err.ToString());
            }
            catch
            {
                Console.WriteLine("Something Wrong");
            }
            DeleteOldSaved();
        }

        private static void DeleteOldSaved()
        {

            DirectoryInfo[] di = new DirectoryInfo(backupServerPath).GetDirectories();
            if (di.Length > numberOfSavesToKeep)
            {
                foreach (var folder in di.OrderByDescending(x => x.LastWriteTime).Skip(numberOfSavesToKeep))
                {
                    folder.Delete(true);
                }
            }
        }
    }
}