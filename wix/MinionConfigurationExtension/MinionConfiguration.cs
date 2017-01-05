using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;


namespace MinionConfigurationExtension {
	public class MinionConfiguration : WixExtension {
		/* 
		 * 	HISTORY
		 *		2016-11-15  mkr service starting and stopping requires a missing C# library/reference. Instead, shellout("sc ...")
		 *		2016-11-15  mkr read the registry for NSIS
		 *		2016-11-13  mkr initiated, just logs the content of c:\ 
		 * 
		*/


        /*
         * Recursivly remove the conf directory.
         * The MSI easily only removes files installed by the MSI.
         * 
         * This CustomAction must be called "late" ("deferred").
         * 
        */
        [CustomAction]
		public static ActionResult NukeConf(Session session) {
			session.Log("MinionConfiguration.cs:: Begin NukeConf");
			///////////
			// System.Collections.Generic.KeyNotFoundException: The given key was not present in the dictionary.
			//var KEEP_CONF = session.CustomActionData["KEEP_CONF"];
			//session.Log("NukeConf:: KEEP_CONF = " + KEEP_CONF);
			//////////
			String soon_conf = @"c:\salt\conf";
			try {
				session.Log("NukeConf:: going to try to Directory delete " + soon_conf);
				Directory.Delete(soon_conf, true);
			} catch (Exception ex) {
				just_ExceptionLog(@"NukeConf tried to delete " + soon_conf, session, ex);
				//return ActionResult.Failure;
			}

			// quirk for https://github.com/markuskramerIgitt/salt-windows-msi/issues/33  Exception: Access to the path 'minion.pem' is denied 
			shellout(session, @"rmdir /s /q " + soon_conf);

			session.Log("MinionConfiguration.cs:: End NukeConf");
			return ActionResult.Success;
		}

		[CustomAction]
		public static ActionResult PrepareEvironmentBeforeInstallation(Session session) {
			/*
			 * This CustomAction must be called "early". 
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
			 * 	remove files, except /salt/conf and /salt/var
			*/
			session.Log("MinionConfiguration.cs:: Begin peel_NSIS");
			RegistryKey reg = Registry.LocalMachine;
			string NSIS_uninstall_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
			bool NSIS_is_installed = reg.OpenSubKey(NSIS_uninstall_key) != null;
			session.Log("peel_NSIS:: NSIS_is_installed = " + NSIS_is_installed);
			if (NSIS_is_installed) {
				session.Log("peel_NSIS:: Going to stop service salt-minion ...");
				shellout(session, "sc stop salt-minion");
				session.Log("peel_NSIS:: Going to delete service salt-minion ...");
				shellout(session, "sc delete salt-minion");

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
			 * Read simple keys from c:/salt/conf/config into WiX properties.
			 * Is there anybody getting  session.Message(InstallMessage.Progress, new Record(2, 1)) ?
			 */
			session.Log("read_SimpleSetting_into_Parameters Begin");
			string configFileFullpath = "c:\\salt\\conf\\minion";
			bool configExists = File.Exists(configFileFullpath);
			if (!configExists) { return true; }
			session.Message(InstallMessage.Progress, new Record(2, 1));
			string[] configText = File.ReadAllLines(configFileFullpath);
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
		public static ActionResult SetRootDir(Session session) /***/ { return save_CustomActionDataKeyValue_to_config_file(session, "MinionRoot", "root_dir"); }
		[CustomAction]
		public static ActionResult SetMaster(Session session) /****/ { return save_CustomActionDataKeyValue_to_config_file(session, "MasterHostname", "master"); }
		[CustomAction]
		public static ActionResult SetMinionId(Session session) /**/ { return save_CustomActionDataKeyValue_to_config_file(session, "MinionHostname", "id"); }

		private static ActionResult save_CustomActionDataKeyValue_to_config_file(Session session, string CustomActionDataKey, string SaltKey) {
			session.Message(InstallMessage.ActionStart, new Record("SetConfigKeyValue1 " + SaltKey, "SetConfigKeyValue2 " + SaltKey, "[1]"));
			session.Message(InstallMessage.Progress, new Record(0, 5, 0, 0));
			session.Log("save_CustomActionDataKeyValue_to_config_file " + SaltKey);
			string CustomActionData_value;
			try {
				CustomActionData_value = session.CustomActionData[CustomActionDataKey];
			} catch (Exception ex) { just_ExceptionLog("Getting CustomActionData " + CustomActionDataKey, session, ex); return ActionResult.Failure; }
			session.Message(InstallMessage.Progress, new Record(2, 1));
			// pattern description
			// ^        start of line
			// #*       a comment hashmark would be removed, if present
			//          anything after the colon is ignored and would be removed 
			string pattern = "^#*" + SaltKey + ":";
			string replacement = String.Format(SaltKey + ": {0}", CustomActionData_value);
			ActionResult result = replace_pattern_in_config_file(session, pattern, replacement);
			session.Message(InstallMessage.Progress, new Record(2, 1));
			session.Log("save_CustomActionDataKeyValue_to_config_file End");
			return result;
		}

		private static ActionResult replace_pattern_in_config_file(Session session, string pattern, string replacement) {
			/*
			 * the pattern finds assigment and assigment commented out:
			 *    #a:b
			 *    a:b  
			 * Only replace the first match, blank out all others
			 */
			string configFileFullPath = getConfigFileLocation(session);
			string[] configText = File.ReadAllLines(configFileFullPath);
			session.Message(InstallMessage.Progress, new Record(2, 1));
			session.Log("replace_pattern_in_config_file..config file    {0}", configFileFullPath);
			session.Message(InstallMessage.Progress, new Record(2, 1));
			try {
				bool never_found_the_pattern = true;
				for (int i = 0; i < configText.Length; i++) {
					if (Regex.IsMatch(configText[i], pattern)) {
						if (never_found_the_pattern) {
							never_found_the_pattern = false;
							session.Log("replace_pattern_in_config_file..pattern        {0}", pattern);
							session.Log("replace_pattern_in_config_file..matched  line  {0}", configText[i]);
							session.Log("replace_pattern_in_config_file..replaced line  {0}", replacement);
							configText[i] = replacement + "\n";
						} else {
							configText[i] = "\n";  // only assign the the config variable once
						}
					}
				}
			} catch (Exception ex) { just_ExceptionLog("Looping Regexp", session, ex); return ActionResult.Failure; }
			session.Message(InstallMessage.Progress, new Record(2, 1));
			try {
				File.WriteAllLines(configFileFullPath, configText);
			} catch (Exception ex) { just_ExceptionLog("Writing to file", session, ex); return ActionResult.Failure; }
			return ActionResult.Success;
		}


		private static void shellout(Session session, string s) {
			// This is a handmade shellout routine
			session.Log("shellout about to try " + s);
			try {
				System.Diagnostics.Process process = new System.Diagnostics.Process();
				System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
				startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
				startInfo.FileName = "cmd.exe";
				startInfo.Arguments = "/C " + s;
				process.StartInfo = startInfo;
				process.Start();
				process.WaitForExit();
			} catch (Exception ex) {
				just_ExceptionLog("shellout tried " + s, session, ex);
			}
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
