using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Tools.WindowsInstallerXml;


namespace MinionConfigurationExtension {
	public class MinionConfiguration : WixExtension {
		/* 
		 * 	HISTORY
		 *		2016-11-15  mkr service starting and stopping requires a missing C# library/reference. Instead, shellout("sc ...")
		 *		2016-11-15  mkr read the registry for NSIS
		 *		2016-11-13  mkr initiated, just logs the content of c:\ 
		 * 
		*/


		[CustomAction]
		public static ActionResult PrepareEvironmentBeforeInstallation(Session session) {
			/*
			 * The function must be called "early". 
			 * 
			 * Read the comments for PrepareEvironmentBeforeInstallation in wix/MinionMSI/Product.wxs
			*/
			session.Log("MinionConfiguration.cs:: Begin PrepareEvironmentBeforeInstallation");
			if (!peel_NSIS(session)) return ActionResult.Failure;
			if (!read_SimpleSetting_into_Property(session)) return ActionResult.Failure;
			session.Log("MinionConfiguration.cs:: End PrepareEvironmentBeforeInstallation");
			return ActionResult.Success;
		}

		private static bool peel_NSIS(Session session) {
			/*
			 * If NSIS is installed:
			 * 	remove salt-minion service, 
			 * 	remove registry
			 * 	emove files, except /salt/conf and /salt/var
			*/
			session.Log("MinionConfiguration.cs:: Begin peel_NSIS");
			RegistryKey reg = Registry.LocalMachine;
			string NSIS_uninstall_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
			bool NSIS_is_installed = reg.OpenSubKey(NSIS_uninstall_key) != null;
			session.Log("peel_NSIS:: NSIS_is_installed = " + NSIS_is_installed);
			if (NSIS_is_installed) {
				session.Log("peel_NSIS:: Going to stop service salt-minion ...");
				shellout("sc stop salt-minion");
				session.Log("peel_NSIS:: Going to delete service salt-minion ...");
				shellout("sc delete salt-minion");

				session.Log("peel_NSIS:: Going to delete ARM registry entry for salt-minion ...");
				try { reg.DeleteSubKeyTree(NSIS_uninstall_key); } catch (Exception) { ;}

				session.Log("peel_NSIS:: Going to delete files ...");
				try { Directory.Delete(@"c:\salt\bin", true); } catch (Exception) { ;}
				try { File.Delete(@"c:\salt\uninst.exe"); } catch (Exception) { ;}
				try { File.Delete(@"c:\salt\nssm.exe"); } catch (Exception) { ;}
				try { foreach (FileInfo fi in new DirectoryInfo(@"c:\salt").GetFiles("salt*.*")) { fi.Delete(); } } catch (Exception) { ;}
			}
			session.Log("MinionConfiguration.cs:: End peel_NSIS");
			return true;
		}


		private static bool read_SimpleSetting_into_Property(Session session) {
			/*
			 * Read simple keys from c:/salt/conf/config file into memory.
			 * Is there anybody getting  session.Message(InstallMessage.Progress, new Record(2, 1)) ?
			 */
			session.Log("read_SimpleSetting_into_Parameters Begin");
			string[] configText = ReadFileContent(session, "c:\\salt\\conf\\minion"); // hack
			session.Message(InstallMessage.Progress, new Record(2, 1));
			try {
				Regex r = new Regex(@"^([a-zA-Z_]+):\s*([0-9a-zA-Z_.-]+)\s*$");
				foreach (string line in configText) {
					if (r.IsMatch(line)) {
						Match m = r.Match(line);
						string key = m.Groups[1].ToString();
						string value = m.Groups[2].ToString();
						session.Log("read_SimpleSetting_into_Property key " + key);
						session.Log("read_SimpleSetting_into_Property val " + value);
						if (key == "master") /**/ { session["MASTER_HOSTNAME"] = value; }
						if (key == "id") /******/ { session["MINION_HOSTNAME"] = value; }
					}
				}
			} catch (Exception ex) { return False_after_ExceptionLog("Looping Regexp", session, ex); }
			session.Message(InstallMessage.Progress, new Record(2, 1));
			session.Log("read_SimpleSetting_into_Parameters End");
			return true;
		}


		// Save user input to conf/minion settings
		[CustomAction]
		public static ActionResult SetRootDir(Session session) /***/ { return SaveConfigKeyToFile("MinionRoot", "root_dir", session); }
		[CustomAction]
		public static ActionResult SetMaster(Session session) /****/ { return SaveConfigKeyToFile("MasterHostname", "master", session); }
		[CustomAction]
		public static ActionResult SetMinionId(Session session) /**/ { return SaveConfigKeyToFile("MinionHostname", "id", session); }

		private static ActionResult SaveConfigKeyToFile(string CustomActionDataKey, string SaltKey, Session session) {
			session.Message(InstallMessage.ActionStart, new Record("SetConfigKeyValue1 " + SaltKey, "SetConfigKeyValue2 " + SaltKey, "[1]"));
			session.Message(InstallMessage.Progress, new Record(0, 5, 0, 0));
			session.Log("Begin SaveConfigKeyToFile " + SaltKey);
			string value22;
			try {
				value22 = session.CustomActionData[CustomActionDataKey];
			} catch (Exception ex) { just_ExceptionLog("Getting CustomActionData " + CustomActionDataKey, session, ex); return ActionResult.Failure; }
			session.Message(InstallMessage.Progress, new Record(2, 1));
			ActionResult result = saveKeyValueToFile(session, "^" + SaltKey + ":", String.Format(SaltKey + ": {0}\n", value22));
			session.Message(InstallMessage.Progress, new Record(2, 1));
			session.Log("End SaveConfigKeyToFile " + SaltKey);
			return result;
		}

		private static ActionResult saveKeyValueToFile(Session session, string pattern, string replacement) {
			string configFileFullPath = getConfigFileLocation(session);
			string[] configText = ReadFileContent(session, configFileFullPath);
			session.Message(InstallMessage.Progress, new Record(2, 1));
			session.Log("Config file: {0}", configFileFullPath);
			session.Message(InstallMessage.Progress, new Record(2, 1));
			try {
				for (int i = 0; i < configText.Length; i++) {
					if (Regex.IsMatch(configText[i], pattern)) {
						configText[i] = replacement;
						session.Log("Set line: {0}", configText[i]);
					}
				}
			} catch (Exception ex) { just_ExceptionLog("Looping Regexp", session, ex); return ActionResult.Failure; }
			session.Message(InstallMessage.Progress, new Record(2, 1));
			try {
				File.WriteAllLines(configFileFullPath, configText);
			} catch (Exception ex) { just_ExceptionLog("Writing to file", session, ex); return ActionResult.Failure; }
			return ActionResult.Success;
		}


		private static void shellout(string s) {
			// This is a handmade shellout routine
			System.Diagnostics.Process process = new System.Diagnostics.Process();
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
			startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";
			startInfo.Arguments = "/C " + s;
			process.StartInfo = startInfo;
			process.Start();
			process.WaitForExit();
		}


		private static string[] ReadFileContent(Session session, string config_file_path) {
			session.Log("ReadFileContent Begin");
			session.Log("ReadFileContent " + config_file_path);
			string[] configText;
			session.Message(InstallMessage.Progress, new Record(2, 1));
			session.Log("Config file: {0}", config_file_path);
			try {
				configText = File.ReadAllLines(config_file_path);
			} catch (Exception ex) { just_ExceptionLog("Reading from file", session, ex); throw ex; }
			session.Log("ReadFileContent End");
			return configText;
		}

		private static string getConfigFileLocation(Session session) {
			string config;
			string rootDir;
			session.Log("getConfigFileLocation Start");
			try {
				rootDir = session.CustomActionData["MinionRoot"];
			} catch (Exception ex) { just_ExceptionLog("Getting CustomActionData MinionRoot", session, ex); throw ex; }

			try {
				config = rootDir + "conf\\minion";
			} catch (Exception ex) { just_ExceptionLog("Concatening config file name", session, ex); throw ex; }
			session.Log("getConfigFileLocation Stop");
			return config;
		}
		private static void just_ExceptionLog(string description, Session session, Exception ex) {
			session.Log(description);
			session.Log("Exception: {0}", ex.Message.ToString());
			session.Log(ex.StackTrace.ToString());
		}
		private static bool False_after_ExceptionLog(string description, Session session, Exception ex) {
			just_ExceptionLog(description, session, ex);
			return false;
		}










	}

}
