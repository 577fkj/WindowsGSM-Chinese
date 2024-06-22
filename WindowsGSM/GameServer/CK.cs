using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System;
using WindowsGSM.Functions;
using Discord;
using System.Net.Sockets;
using System.Windows.Documents;
using System.Linq;

namespace WindowsGSM.GameServer
{
	class CK
	{

		// �洢�������������ݵĶ���
		private readonly ServerConfig _serverData;

		// ������Ϣ��֪ͨ��Ϣ
		public string Error;
		public string Notice;

		// ������ȫ��������·�����Ƿ�����Ƕ�����̨���˿ں���������ѯ�����ȷ�������Ϣ
		public const string FullName = "���Ļ����� ר�÷�����";
		public string StartPath = @"Launch.bat";
		public bool AllowsEmbedConsole = true;
		public int PortIncrements = 2;
		public dynamic QueryMethod = new Query.A2S();

		// Ĭ��������˿ںš���ѯ�˿ڡ�Ĭ�ϵ�ͼ���������������������Ӧ�� ID
		public string Port = "9000";
		public string QueryPort = "9100";
		public string Defaultmap = "map";
		public string Maxplayers = "100";
		public string Additional = $""; // ����ķ�������������
		public string AppId = "1963720";

		// ���캯������Ҫ����������������ݶ���
		public CK(Functions.ServerConfig serverData)
		{
			_serverData = serverData;
		}

		// - �ڰ�װ��Ϊ��Ϸ����������һ��Ĭ�ϵ� cfg
		public async void CreateServerCFG()
		{

		}

		// ��������������
		// - Start server function, return its Process to WindowsGSM
		public async Task<Process> Start()
		{

			string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
			if (!File.Exists(shipExePath))
			{
				Error = $"{Path.GetFileName(shipExePath)} δ�ҵ� ({shipExePath})";
				return null;
			}
			string param = $"-batchmode {_serverData.ServerParam}" + (!AllowsEmbedConsole ? " -log" : string.Empty);
			var p = new Process
			{
				StartInfo =
				{
					WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
					FileName = shipExePath,
					Arguments = param,
					WindowStyle = ProcessWindowStyle.Minimized,
					UseShellExecute = false
				},
				EnableRaisingEvents = true
			};

			// Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
			if (AllowsEmbedConsole)
			{
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.RedirectStandardInput = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;
				var serverConsole = new ServerConsole(_serverData.ServerID);
				p.OutputDataReceived += serverConsole.AddOutput;
				p.ErrorDataReceived += serverConsole.AddOutput;

				// Start Process
				try
				{
					p.Start();
				}
				catch (Exception e)
				{
					Error = e.Message;
					return null; // return null if fail to start
				}

				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				return p;
			}

			// Start Process
			try
			{
				p.Start();
				return p;
			}
			catch (Exception e)
			{
				Error = e.Message;
				return null; // return null if fail to start
			}
		}


		public async Task Stop(Process p)
		{
			Process coreKeeperProcess = Process.GetProcessesByName("CoreKeeperServer").FirstOrDefault();

			await Task.Run(() =>
			{
				Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
				Functions.ServerConsole.SendWaitToMainWindow("q");
				if (coreKeeperProcess != null&&!coreKeeperProcess.HasExited)
				{
					coreKeeperProcess.Kill();
				}
			});
			await Task.Delay(20000);
		}
		// ��װ��Ϸ�����
		public async Task<Process> Install()
		{
			var steamCMD = new Installer.SteamCMD();
			// ʹ�� SteamCMD ��װ�����
			Process p = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId);
			Error = steamCMD.Error;

			return p;
		}

		// ������Ϸ�����
		public async Task<Process> Update(bool validate = false, string custom = null)
		{
			// ʹ�� SteamCMD ���·����
			var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom);
			Error = error;
			return p;
		}

		// �ڷ������ļ����м����Ϸ������Ƿ�����ȷ��װ
		public bool IsInstallValid()
		{
			return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, "CoreKeeperServer.exe"));
		}

		public bool IsImportValid(string path)
		{
			string exePath = Path.Combine(path, "CoreKeeperServer.exe");
			Error = $"��Ч·�����Ҳ��� {Path.GetFileName(exePath)}";
			return File.Exists(exePath);
		}

		// ��ȡ������Ϸ����˵İ汾��
		public string GetLocalBuild()
		{
			var steamCMD = new Installer.SteamCMD();
			return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
		}

		// ��ȡ�ٷ���Ϸ����˵İ汾��
		public async Task<string> GetRemoteBuild()
		{
			var steamCMD = new Installer.SteamCMD();
			return await steamCMD.GetRemoteBuild(AppId);
		}
	}
}