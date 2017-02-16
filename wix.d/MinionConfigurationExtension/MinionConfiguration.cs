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
		 * Must only be called when KEEP_CONFIG=0
		 * 
		 * Recursivly remove the conf directory.
		 * The MSI easily only removes files installed by the MSI.
		 * 
		 * This CustomAction must be immediate.
		 * 
		*/
		[CustomAction]
		public static ActionResult NukeConf(Session session) {
			session.Log("MinionConfiguration.cs:: Begin NukeConf");
			String soon_conf = @"c:\salt\conf"; //TODO
			String root_dir = ""; 
			try {
				root_dir = session["INSTALLFOLDER"];
			} catch (Exception ex) {
				just_ExceptionLog("FATAL ERROR while getting Property INSTALLFOLDER", session, ex); 
			}
			session.Log("NukeConf::  root_dir = " + root_dir);
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

		[CustomAction]
		public static ActionResult HACKStopSaltOnUninstall(Session session) {
			session.Log("MinionConfiguration.cs:: Begin HACKStopSaltOnUninstall");
			shellout(session, "sc stop salt-minion");
			session.Log("MinionConfiguration.cs:: End HACKStopSaltOnUninstall");
			return ActionResult.Success;
		}
		

		private static bool peel_NSIS(Session session) {
			/*
			 * If NSIS is installed:
			 * 	remove salt-minion service, 
			 * 	remove registry
			 * 	remove files, except /salt/conf and /salt/var
			 * 	
			 * 	all fixed path are OK here.
			 * 	The msi is never peeled.
			*/
			session.Log("MinionConfiguration.cs:: Begin peel_NSIS");
			session.Log("Environment.Version = " + Environment.Version);
			if (IntPtr.Size == 8) {
				session.Log("probably 64 bit process");
			} else {
				session.Log("probably 32 bit process");
			}
			RegistryKey reg = Registry.LocalMachine;
			// (Only?) in regedit this is under    SOFTWARE\WoW6432Node
			string Salt_uninstall_regpath64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
			string Salt_uninstall_regpath32 = @"SOFTWARE\WoW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
			var SaltRegSubkey64 = reg.OpenSubKey(Salt_uninstall_regpath64);
			var SaltRegSubkey32 = reg.OpenSubKey(Salt_uninstall_regpath32);
			bool NSIS_is_installed64 = (SaltRegSubkey64 != null) && SaltRegSubkey64.GetValue("UninstallString").ToString().Equals(@"c:\salt\uninst.exe", StringComparison.OrdinalIgnoreCase);
			bool NSIS_is_installed32 = (SaltRegSubkey32 != null) && SaltRegSubkey32.GetValue("UninstallString").ToString().Equals(@"c:\salt\uninst.exe", StringComparison.OrdinalIgnoreCase);
			session.Log("peel_NSIS:: NSIS_is_installed64 = " + NSIS_is_installed64);
			session.Log("peel_NSIS:: NSIS_is_installed32 = " + NSIS_is_installed32);
			if (NSIS_is_installed64 || NSIS_is_installed32) {
				session.Log("peel_NSIS:: Going to stop service salt-minion ...");
				shellout(session, "sc stop salt-minion");
				session.Log("peel_NSIS:: Going to delete service salt-minion ...");
				shellout(session, "sc delete salt-minion");

				session.Log("peel_NSIS:: Going to delete ARP registry64 entry for salt-minion ...");
				try { reg.DeleteSubKeyTree(Salt_uninstall_regpath64); } catch (Exception ex) { just_ExceptionLog("", session, ex); }
				session.Log("peel_NSIS:: Going to delete ARP registry32 entry for salt-minion ...");
				try { reg.DeleteSubKeyTree(Salt_uninstall_regpath32); } catch (Exception ex) { just_ExceptionLog("", session, ex); }

				session.Log("peel_NSIS:: Going to delete files ...");
				try { Directory.Delete(@"c:\salt\bin", true); }  catch (Exception ex) {just_ExceptionLog("", session, ex);}
				try { File.Delete(@"c:\salt\uninst.exe"); } catch (Exception ex) { just_ExceptionLog("", session, ex); }
				try { File.Delete(@"c:\salt\nssm.exe"); } catch (Exception ex) { just_ExceptionLog("", session, ex); }
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
			string configFileFullpath = "c:\\salt\\conf\\minion"; // TODO
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


		// Must have this signature or cannot uninstall not even write to the log
		[CustomAction]
		public static ActionResult SetRootDir(Session session) /***/ {
			string rootDir;
			try {
				rootDir = session.CustomActionData["root_dir"];
			} catch (Exception ex) { just_ExceptionLog("Getting CustomActionData " + "root_dir", session, ex); return ActionResult.Failure; }

			session.Log(@"looking for NSIS configuration in c:\salt");
			re_use_NSIS_config_folder(session, @"c:\salt\", rootDir); // This is intentionally using the fixed NSIS installation path

			return save_CustomActionDataKeyValue_to_config_file(session, "root_dir");
		}
		// todo refactor the two below 2 Set???? procedures into the above
		[CustomAction]
		public static ActionResult SetMaster(Session session) /****/ {
			return save_CustomActionDataKeyValue_to_config_file(session, "master");
		}
		[CustomAction]
		public static ActionResult SetMinionId(Session session) /**/ {
			return save_CustomActionDataKeyValue_to_config_file(session, "id");
		}

		private static ActionResult save_CustomActionDataKeyValue_to_config_file(Session session, string SaltKey) {
			session.Message(InstallMessage.ActionStart, new Record("SetConfigKeyValue1 " + SaltKey, "SetConfigKeyValue2 " + SaltKey, "[1]"));
			session.Message(InstallMessage.Progress, new Record(0, 5, 0, 0));
			session.Log("save_CustomActionDataKeyValue_to_config_file " + SaltKey);
			string CustomActionData_value;
			try {
				CustomActionData_value = session.CustomActionData[SaltKey];
			} catch (Exception ex) { just_ExceptionLog("Getting CustomActionData " + SaltKey, session, ex); return ActionResult.Failure; }
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

		private static void re_use_NSIS_config_folder(Session session, string old_install_path, string new_install_path) {
			session.Log("re_use_NSIS_config_folder BEGIN");
			session.Log(old_install_path  + " to " + new_install_path );
			if (old_install_path.Equals(new_install_path, StringComparison.InvariantCultureIgnoreCase)) {
				// same location!
				session.Log(old_install_path + " == " + new_install_path);
				return;
			}
			log_config_folder_content(session, old_install_path);
			if (! (File.Exists(minion_pem(old_install_path))
				&& File.Exists(minion_pup(old_install_path))
				&& File.Exists(minion_master_pup(old_install_path)))) {
				session.Log("There is no complete configuration at " + old_install_path);
				session.Log("re_use_NSIS_config_folder END PREMATURLY");
				return;
			}

			// Now we assume:
			//   there is a NSIS configuration.
			//   this is not an msi upgrade but a first install
			// Therefore move configuation into the target install dir


			log_config_folder_content(session, new_install_path);
			if (File.Exists(minion_pem(new_install_path))
				|| File.Exists(minion_pup(new_install_path)) 
				|| File.Exists(minion_master_pup(new_install_path))) {
				session.Log("There is a configuration at " + new_install_path);
				session.Log("No move");
				session.Log("re_use_NSIS_config_folder END PREMATURLY");
				return;
			}
			session.Log(old_install_path + "conf" + " now moving to " + new_install_path + "conf");
			// minion.pem permission do not allow to move it
			// change permission
			// move files or folder?
			session.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! move !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
			if (false) 
				File.Move(minion_pem(old_install_path), minion_pem(new_install_path));
			
			session.Log("re_use_NSIS_config_folder END");
		}

		private static string minion_config_file(string config_folder) { return config_folder + @"conf\minion"; }
		private static string minion_d_folder(string config_folder) { return config_folder + @"conf\minion.d"; }
		private static string pki_minion_folder(string config_folder) { return config_folder + @"conf\pki\minion"; }
		private static string minion_pem(string config_folder) { return config_folder + @"conf\pki\minion\minion.pem"; }
		private static string minion_pup(string config_folder) { return config_folder + @"conf\pki\minion\minion.pub"; }
		private static string minion_master_pup(string config_folder) { return config_folder + @"conf\pki\minion\minion_master.pub"; }


		private static bool log_config_folder_content(Session session, string potential_config_folder) {
			session.Log("potential_config_folder         = " + potential_config_folder);
			session.Log("potential_config_folder_exists  = " + Directory.Exists(potential_config_folder));
			if (!Directory.Exists(potential_config_folder)) { 
				return false;
			}

			session.Log("salt_minion_config_file        = " + minion_config_file(potential_config_folder));
			session.Log("salt_minion_config_file_exists = " + File.Exists(minion_config_file(potential_config_folder)));

			session.Log("minion_d_folder        = " + minion_d_folder(potential_config_folder));
			session.Log("minion_d_folder_exists = " + Directory.Exists(minion_d_folder(potential_config_folder)));

			session.Log("pki_minion_folder        = " + pki_minion_folder(potential_config_folder));
			session.Log("pki_minion_folder_exists = " + Directory.Exists(pki_minion_folder(potential_config_folder)));
			if (!Directory.Exists(pki_minion_folder(potential_config_folder))) {
				return false;
			}

			session.Log("minion_pem        = " + minion_pem(potential_config_folder));
			session.Log("minion_pem_exists = " + File.Exists(minion_pem(potential_config_folder)));

			session.Log("minion_pub        = " + minion_pup(potential_config_folder));
			session.Log("minion_pub_exists = " + File.Exists(minion_pup(potential_config_folder)));

			session.Log("minion_master_pub        = " + minion_master_pup(potential_config_folder));
			session.Log("minion_master_pub_exists = " + File.Exists(minion_master_pup(potential_config_folder)));
			return true;
		}
		/*
         * root_dir = INSTALLDIR  the planned installation dir.
		 * 
         * I need to move the old installation dir to the new installation dir.
         * 
		 * When?
		 *  - After NukeConf
		 *  - After the user has given INSTALLDIR
		 * 
         * How do I get the old installation dir previous_root_dir?
         * - try c:\salt
         * - read content from KEEP_CONFIG_File
         * 
         * if config at previous_root_dir then
         *   move to root_dir
         */
		private static string getConfigFileLocation(Session session) {
			session.Log("getConfigFileLocation BEGIN ");

			string rootDir;
			string salt_config_file;

			try {
				rootDir = session.CustomActionData["root_dir"];
			} catch (Exception ex) { just_ExceptionLog("FATAL ERROR while getting CustomActionData root_dir", session, ex); throw ex; }
			session.Log("INSTALLFOLDER == rootDir = " + rootDir);

			salt_config_file = rootDir + "conf\\minion";

			session.Log("getConfigFileLocation END");
			return salt_config_file;
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
