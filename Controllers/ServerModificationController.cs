using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;
using System.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;

namespace ArksZooAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ServerModificationController : ControllerBase
    {
        //GET ServerModification
        [HttpGet]
        public Dictionary<string, string> CurrentConfig()
        {
            NameValueCollection appSettings;
            appSettings = ConfigurationManager.AppSettings;
            var valuePair = new Dictionary<string, string>();
            foreach (string key in ConfigurationManager.AppSettings)
            {
                valuePair.Add(key, appSettings.Get(key));
            }
            return valuePair;
        }

        //POST ServerModification
        [HttpPost]
        public string UpdateConfig(Models.ServerConfig newConfig)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["GamePath"].Value = newConfig.GamePath;
            config.AppSettings.Settings["BackupPath"].Value = newConfig.BackupPath;
            config.AppSettings.Settings["BackupInterval"].Value = newConfig.BackupInterval.ToString();
            config.AppSettings.Settings["HoursSave"].Value = newConfig.HoursSave.ToString();
            config.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection("appSettings");
            return "Settings updated";
        }
    }

    //GET IsServerOn
    [Route("[controller]")]
    [ApiController]
    public class IsServerOnController : ControllerBase
    {
        //Work in progress
        //TODO need to check the server connection status, not current running process
        [HttpGet]
        public Boolean ServerAvailability(string ipaddress, int port)
        {
            
            Process[] pname = Process.GetProcessesByName("ShooterGameServer");
            if (pname.Length == 0)
                return false;
            else
                return true;
            /*
            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(ipaddress, port);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            */
        }
    }

    [Route("[controller]")]
    [ApiController]
    public class StartServerController : ControllerBase
    {
        private static readonly HttpClient client = new HttpClient();

        //GET startServer/mapName}
        [HttpGet("{mapName}")]
        public string StartServer(string mapName)
        {
            /*
            var filePath = ConfigurationManager.AppSettings["gamePath"].ToString() + "/Binaries/Win64/Start_"+mapName+".bat";
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(filePath);
                processInfo.UseShellExecute = false;
                Process batchProcess = new Process();
                batchProcess.StartInfo = processInfo;
                batchProcess.Start();
                return "Start_" + mapName + ".bat Map started successfully";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
            BackupController backupController = new BackupController();
            */
            try
            {
                var batchFileLocation = ConfigurationManager.AppSettings["gamePath"].ToString() + "/Binaries/Win64/Start_" + mapName + ".bat";
                Process p = new Process();
                p.StartInfo.FileName = batchFileLocation;
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(batchFileLocation);
                p.StartInfo.UseShellExecute = false;
                p.Start();
                //backupController.StartBackup();
                return "Start_" + mapName + ".bat Map started successfully";
            }
            catch (Exception e)
            {
                return e.ToString();
            }

            //Process.Start(filePath);
            //var responseString = await client.GetStringAsync("https://localhost:44347/Backup/start-backup");

            //Process.Start("cmd.exe", "cd "+ ConfigurationManager.AppSettings["gamePath"] + " Binaries/Win64");
            //CALL Start_TheIsland.bat
        }
    }

    [Route("[controller]")]
    [ApiController]
    public class StopServerController : ControllerBase
    {
        //GET stopserver
        [HttpGet]
        public void StopServer()
        {
            foreach (var process in Process.GetProcessesByName("ShooterGameServer"))
            {
                process.Kill();
            }
        }
    }
}