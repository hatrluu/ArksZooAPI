using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using CoreRCON;
using CoreRCON.Parsers.Standard;
using System.Net;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace ArksZooAPI.Controllers
{
    [Route("server")]
    [ApiController]
    public class ServerModificationController : ControllerBase
    {
        public string ipaddress = "", password = "";
        public int port = 0;

        public enum Status
        {
            OFFLINE,
            ONLINE,
            STARTING
        }

        //GET server/settings
        [HttpGet]
        [Route("settings")]
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

        //POST server/settings
        [HttpPost]
        [Route("settings")]
        public IActionResult UpdateConfig(Models.ServerConfig newConfig)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            try
            {
                config.AppSettings.Settings["MapName"].Value = newConfig.MapName.Trim();
                config.AppSettings.Settings["GamePath"].Value = newConfig.GamePath.Trim();
                config.AppSettings.Settings["BackupPath"].Value = newConfig.BackupPath.Trim();
                config.AppSettings.Settings["BackupInterval"].Value = newConfig.BackupInterval.ToString();
                config.AppSettings.Settings["HoursSave"].Value = newConfig.HoursSave.ToString();
                config.Save(ConfigurationSaveMode.Modified);

                ConfigurationManager.RefreshSection("appSettings");
                return Ok("Settings updated");
            } 
            catch (Exception e)
            {
                return BadRequest(e.ToString());
            }

        }

        //GET server/status
        //Work in progress
        //TODO need to check the server connection status, not current running process
        [HttpGet]
        [Route("status")]
        public async Task<IActionResult> ServerStatus()
        {
            Debug.WriteLine("Status");
            var rconStatus = await RunRCON("");
            if (rconStatus == Status.ONLINE)
            {
                return Ok("Server started");
            }
            else if (rconStatus == Status.OFFLINE)
            {
                return NoContent();
            }
            else
            {
                return StatusCode(StatusCodes.Status202Accepted);
            }
        }

        //GET server/start/mapName
        [HttpGet]
        [Route("start/{MapName}")]
        public IActionResult StartServer(string MapName)
        {
            Debug.WriteLine("Start Map");
           
            try
            {
                var batchFileLocation = ConfigurationManager.AppSettings["gamePath"].ToString() + "/Binaries/Win64/Start_" + MapName + ".bat";
                Process p = new Process();
                p.StartInfo.FileName = batchFileLocation;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(batchFileLocation);
                p.StartInfo.UseShellExecute = false;
                p.Start();

                return Ok("Start_" + MapName + ".bat Map started successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("{0:hh:mm:ss} {1}", DateTime.Now, ex.ToString()));
                return StatusCode(StatusCodes.Status500InternalServerError, ex.ToString());
            }
        }

        //GET server/stop
        [HttpGet]
        [Route("stop")]
        public async Task<IActionResult> StopServer()
        {
            Debug.WriteLine("Stop Server");
            try
            {
                var rconStatus = await RunRCON("");
                if (rconStatus == Status.ONLINE)
                {
                    await SaveWorld();
                    Thread.Sleep(1000);
                }
                foreach (var process in Process.GetProcessesByName("ShooterGameServer"))
                {
                    process.Kill();
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.ToString());
            }
        }

        //GET server/save
        [HttpGet]
        [Route("save")]
        public async Task<IActionResult> SaveWorld()
        {
            Debug.WriteLine("Save World");
            string command = "saveworld";
            var rconStatus = await RunRCON(command);
            if (rconStatus == Status.ONLINE)
            {
                return Ok();
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<Status> RunRCON(string command)
        {
            ipaddress = ConfigurationManager.AppSettings["IPAddress"];
            password = ConfigurationManager.AppSettings["ServerAdminPassword"];
            port = Int32.Parse(ConfigurationManager.AppSettings["RConPort"]);
            var endpoint = new IPEndPoint(
                IPAddress.Parse(ipaddress),
                port
            );
            var rcon = new RCON(endpoint, password);
            
            Process[] pname = Process.GetProcessesByName("ShooterGameServer");
            if (pname.Length == 0)
            {
                return Status.OFFLINE;
            }
            else
            {
                try
                {
                    await rcon.ConnectAsync();
                    Debug.WriteLine("Rcon connected");
                    if (!String.IsNullOrEmpty(command)){
                        await rcon.SendCommandAsync(command);
                    }
                    return Status.ONLINE;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: No connection - {0}", ex.ToString());
                    return Status.STARTING;
                }
            }
            
        }

        //GET server/stats
        [HttpGet]
        [Route("stats")]
        public async  Task<IActionResult> GetServerStats()
        {
            var rconStatus = await RunRCON("");
            if(rconStatus == Status.ONLINE)
            {
                try
                {
                    IPEndPoint ip = new IPEndPoint(Dns.GetHostAddresses("arks-zoo.xyz")[0], 27015);

                    A2S_INFO a2S_INFO = new A2S_INFO(ip);
                    A2S_PLAYER a2S_PLAYER = new A2S_PLAYER(ip);

                    if(a2S_INFO.Players > 0)
                    {
                        ArrayList playerStats = new ArrayList();
                        playerStats.Add(a2S_INFO.Players);
                        playerStats.Add(a2S_INFO.MaxPlayers);
                        playerStats.Add(a2S_PLAYER.Players);

                        return Ok(playerStats);
                    }
                    else
                    {
                        return NoContent();
                    }
                }
                catch (SocketException ex)
                {
                    Debug.WriteLine(ex.ToString());
                    return NoContent();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    return NoContent();
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status404NotFound);
            }
        }
    }
}