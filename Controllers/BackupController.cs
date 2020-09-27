using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Timers;
using System.Diagnostics;
using System.Configuration;

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
        private static bool backupStatus = false;

        [HttpGet]
        [Route("current")]
        public IActionResult CurrentServerSave()
        {
            string currentSaveGamePath = ConfigurationManager.AppSettings["GamePath"] + "\\Saved\\SavedArks\\"+ConfigurationManager.AppSettings["MapName"] +".ark";
            FileSystemInfo file = new DirectoryInfo(currentSaveGamePath);

            return Ok(file.LastWriteTime);
        }

        //GET backup/latest
        [HttpGet]
        [Route("latest")]
        public IActionResult LatestBackup()
        {
            //Update path
            backupServerPath = ConfigurationManager.AppSettings["BackupPath"];

            //Is try catch better here?
            DirectoryInfo[] di = new DirectoryInfo(backupServerPath).GetDirectories();
            if(di.Length != 0)
            {
                string latestBackup = "";
                var latestFile = di.OrderByDescending(x => x.LastWriteTime).First();
                string[] fileNameArr = latestFile.Name.Split('-');
                latestBackup = fileNameArr[1] + " " + fileNameArr[2];
                System.Diagnostics.Debug.WriteLine(latestBackup);
                return Ok(latestBackup);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Empty Folder, No save here"); 
                return NoContent();
            }
        }

        //GET backup/status
        [HttpGet]
        [Route("status")]
        public bool BackupStatus()
        {
            return backupStatus ? true : false;
        }

        //GET backup/start
        [HttpGet]
        [Route("start")]
        public IActionResult StartBackup()
        {
            int backupInterval = Int32.Parse(ConfigurationManager.AppSettings["BackupInterval"]);
            int hoursSave = Int32.Parse(ConfigurationManager.AppSettings["HoursSave"]);

            numberOfSavesToKeep = hoursSave * 60 / backupInterval;

            TimeChecker(backupInterval);
            backupStatus = true;
            return Ok(string.Format("Backup started at {0:hh:mm:ss.fff}", DateTime.Now));
        }

        //GET backup/stop
        [HttpGet]
        [Route("stop")]
        public IActionResult StopBackup()
        {
            saveTimer.Stop();
            saveTimer.Dispose();
            backupStatus = false;
            return Ok(string.Format("Backup ended at {0:hh:mm:ss.fff}", DateTime.Now));
        }

        //GET backup/test
        [HttpGet]
        [Route("manual")]
        public IActionResult ManualBackup()
        {
            //Update path
            backupServerPath = ConfigurationManager.AppSettings["BackupPath"];

            string current = currentServerPath;
            string backup = backupServerPath+ "\\";
            DateTime localDateTime = DateTime.Now;
            string backupFolder = "manualbackup-" + localDateTime.ToString("hhmmtt-MMdd");
            string newBackupLocation = backup + backupFolder;

            try
            {
                Directory.CreateDirectory(newBackupLocation);
            
                //Now Create all of the directories
                foreach (string dirPath in Directory.GetDirectories(current, "*", SearchOption.AllDirectories))
                    Directory.CreateDirectory(dirPath.Replace(current, newBackupLocation));

                //Copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(current, "*.*", SearchOption.AllDirectories))
                    System.IO.File.Copy(newPath, newPath.Replace(current, newBackupLocation), true);

                return Ok(string.Format("Backup Completed at {0}", backupFolder));
            }
            catch (IOException err)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,string.Format("{0:hh:mm:ss.fff-MM/dd} IOException {1}", DateTime.Now, err));
            }
            catch (Exception err)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, string.Format("{0:hh:mm:ss.fff-MM/dd} Exception {1}", DateTime.Now, err));
            }

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
            Process[] pname = Process.GetProcessesByName("ShooterGameServer");
            if (pname.Length == 0)
            {
                backupStatus = false;
            } 
            else 
            { 
                //Update path
                backupServerPath = ConfigurationManager.AppSettings["BackupPath"];
                
                string current = currentServerPath;
                string backup = backupServerPath + "\\";
                DateTime localDateTime = DateTime.Now;
                string backupFolder = "backup-" + localDateTime.ToString("hhmmtt-MMdd");
                string newBackupLocation = backup + backupFolder;

                try
                {
                    Directory.CreateDirectory(newBackupLocation);
                    //Now Create all of the directories
                    foreach (string dirPath in Directory.GetDirectories(current, "*", SearchOption.AllDirectories))
                        Directory.CreateDirectory(dirPath.Replace(current, newBackupLocation));

                    //Copy all the files & Replaces any files with the same name
                    foreach (string newPath in Directory.GetFiles(current, "*.*", SearchOption.AllDirectories))
                        System.IO.File.Copy(newPath, newPath.Replace(current, newBackupLocation), true);

                    Debug.WriteLine("{0:hh:mm:ss} Backup Completed at {1}", DateTime.Now, backupFolder);
                }
                catch (IOException err)
                {
                    Debug.WriteLine(err);
                }
                catch (Exception err)
                {
                    Debug.WriteLine(err);
                }
                DeleteOldSaved();
            }
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