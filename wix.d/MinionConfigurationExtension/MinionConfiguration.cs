using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;

// FORMATTING ////////////////////////
// Visual Studio
//   Tools/Options/Text Editor/C#/Tabs                  --> Smart, 2, Insert spaces
//   Tools/Options/Text Editor/C#/Formatting/New Liness --> None


namespace MinionConfigurationExtension {
  public class MinionConfiguration : WixExtension {


    /*
     * Remove 'lifetime' data because the MSI easily only removes 'installtime' data.
     */
    [CustomAction]
    public static ActionResult Uninstall_incl_Config_DECAC(Session session) {
      // Do NOT keep config
      // In fact keep nothing
      session.Log("...Begin Uninstall_incl_Config_DECAC");
      PurgeDir(session, "");  // this means to Purge c:\salt\
      session.Log("...End Uninstall_incl_Config_DECAC");
      return ActionResult.Success;
    }



		[CustomAction]
		public static ActionResult Uninstall_excl_Config_DECAC(Session session) {
			// DO keep config
			/* Selectively delete the var folder.
       * 
       * Directories in var regarded as config:
         c:\salt\var\cache\salt\minion\extmods\
         c:\salt\var\cache\salt\minion\files\
         
         We move the 2 directories out of var, delete var, and move back
      */
			session.Log("...Begin Uninstall_excl_Config_DECAC");
			PurgeDir(session, @"bin");
			// move parts from var into safety
			string safedir = @"c:\salt\_tmp_swap_space\";
			if (Directory.Exists(safedir)) { Directory.Delete(safedir); }
			Directory.CreateDirectory(safedir);
      movedir_fromAbs_toRel(session, @"c:\salt\var\cache\salt\minion\extmods", "extmods", true, safedir);
      movedir_fromAbs_toRel(session, @"c:\salt\var\cache\salt\minion\files", "files", true, safedir);
      // purge var
      PurgeDir(session, @"var");
      // move back
      Directory.CreateDirectory(@"c:\salt\var\cache\salt\minion"); // Directory.Move cannot create dirs
      movedir_fromAbs_toRel(session, @"c:\salt\var\cache\salt\minion\extmods", "extmods", false, safedir);
      movedir_fromAbs_toRel(session, @"c:\salt\var\cache\salt\minion\files", "files", false, safedir);
      Directory.Delete(safedir);

      // log
      session.Log("...End Uninstall_excl_Config_DECAC");
      return ActionResult.Success;
    }

    private static void movedir_fromAbs_toRel(Session session, string abs_from0, string rel_tmp_dir, bool into_safety, string safedir) {
      string abs_from;
      string abs_to;
      if (into_safety) {
        abs_from = abs_from0;
        abs_to = safedir + rel_tmp_dir;
      } else {
        abs_from = safedir + rel_tmp_dir;
        abs_to = abs_from0;
      }

      session.Log("...We may need to move? does directory exist " + abs_from);
      if (Directory.Exists(abs_from)) {
        session.Log(".....yes");
      } else {
        session.Log(".....no");
        return;
      }
      if (Directory.Exists(abs_to)) {
        session.Log("....!I must first delete the TO directory " + abs_to);
        shellout(session, @"rmdir /s /q " + abs_to); 
      }
      // Now move
      try {
        session.Log("...now move to " + abs_to);
        
        Directory.Move(abs_from, abs_to);
        session.Log(".........ok");
      } catch (Exception ex) {
        just_ExceptionLog(@"...moving failed", session, ex);
  }
}

 

    private static void PurgeDir(Session session, string dir_below_salt_root) {
      String abs_dir = @"c:\salt\" + dir_below_salt_root; //TODO use root_dir
      String root_dir = "";
      root_dir = session.CustomActionData["root_dir"];

			if (Directory.Exists(abs_dir)) {
				session.Log("PurgeDir:: about to Directory.delete " + abs_dir);
				Directory.Delete(abs_dir, true);
				session.Log("PurgeDir:: ...OK");
			} else {
				session.Log("PurgeDir:: no Directory " + abs_dir);
			}

			// quirk for https://github.com/markuskramerIgitt/salt-windows-msi/issues/33  Exception: Access to the path 'minion.pem' is denied . Read only!
			shellout(session, @"rmdir /s /q " + abs_dir);
    }



    [CustomAction]
    public static ActionResult ReadConfig_IMCAC(Session session) {
      /*
       * We always call because we cannot know who installed (nsis or msi)
       * 
       */
      session.Log("...Begin ReadConfig_IMCAC");
      determine_master_and_id_IMCAC(session);
      session.Log("...End ReadConfig_IMCAC");
      return ActionResult.Success;
    }


		/*
		 * When installatioin starts,there might be a previous installation.
		 * From the previous installation, we read only two properties, that we present in the installer:
		 *  - master
		 *  - id
		 *  
		 *  This function reads these two properties from 
		 *   - the 2 msi properties:
		 *     - MASTER
		 *     - MINION_ID		    
		 *   - files from a provious installations: 
		 *     - the number of file the function searches depend on CONFIGURATION_TYPE
		 *   - dependend on CONFIGURATION_TYPE, default values can be:
		 *     - master = "salt"
		 *     - id = %hostname%
		 *  
		 *  
		 *  This function writes its results in the 2 msi properties:
		 *   - MASTER
		 *   - MINION_ID
		 *   
		 *   A GUI installation will show these msi properties because this function is called before the GUI.
		 *   
		 */
		private static void determine_master_and_id_IMCAC(Session session) {
      String master_from_previous_installation = "";
      String id_from_previous_installation = "";
			// Read master and id from MINION_CONFIGFILE   
			read_master_and_id_from_file(session, session["MINION_CONFIGFILE"], ref master_from_previous_installation, ref id_from_previous_installation);
			// Read master and id from minion.d/*.conf 
			string MINION_CONFIGDIR = getConfigdDirectoryLocation_IMCAC(session);
			if (Directory.Exists(MINION_CONFIGDIR)) {
				var conf_files = System.IO.Directory.GetFiles(MINION_CONFIGDIR, "*.conf");
				foreach (var conf_file in conf_files) {
					if (conf_file.Equals("_schedule.conf")) { continue; }            // skip _schedule.conf
					read_master_and_id_from_file(session, conf_file, ref master_from_previous_installation, ref id_from_previous_installation);
				}
			}

			if (Directory.Exists(session["INSTALLFOLDER"])) {
				// Log how many files there are in INSTALLFOLDER
				var count_files = Directory.GetFiles(session["INSTALLFOLDER"], "*", SearchOption.AllDirectories).Length;
				session.Log("...counted " + count_files.ToString() + " files in INSTALLFOLDER = " + session["INSTALLFOLDER"]);
			} else {
				// Log there is no INSTALLFOLDER
				session.Log("...no directory INSTALLFOLDER = " + session["INSTALLFOLDER"]);
			}

			session.Log("...CONFIG_TYPE msi property  =" + session["CONFIG_TYPE"]);
			session.Log("...MASTER      msi property  =" + session["MASTER"]);
			session.Log("...MINION_ID   msi property  =" + session["MINION_ID"]);

			/* config types 
			 * https://docs.saltstack.com/en/latest/topics/installation/windows.html#silent-installer-options
			 * 
			 * There are 4 scenarios the installer tries to account for:
1. existing-config (default)
2. custom-config
3. default-config
4. new-config
		 	 */


			if (session["CONFIG_TYPE"] == "Existing") {
				/* ------------------------------------
				 *      1 / 4
				 * ------------------------------------
				 * 
				 * 
This setting makes no changes to the existing config and just upgrades/downgrades salt. 
Makes for easy upgrades. Just run the installer with a silent option. 
If there is no existing config, then the default is used and `master` and `minion id` are applied if passed.
				 */

				// Nothing to do: 
				//  - the installer will lay down the default.
				//  - a msi property is applied if passed
			}

			if (session["CONFIG_TYPE"] == "Custom") {
				/* ----------------------------------
				 *      2 / 4
				 * ----------------------------------
				 * 
This setting will lay down a custom config passed via the command line. Since we want to make sure the custom config is applied correctly, we'll need to back up any existing config.
1. `minion` config renamed to `minion-<timestamp>.bak`
2. `minion_id` file renamed to `minion_id-<timestamp>.bak`
3. `minion.d` directory renamed to `minion.d-<timestamp>.bak`
Then the custom config is laid down by the installer... and `master` and `minion id` should be applied to the custom config if passed.
				 */

				// Nothing to do: 
				//  - the installer will lay down the default.
				//  - a msi property is applied if passed

				// TODO SHOULD happen in WriteConfig
				Backup_configuration_files_from_previous_installation(session);

				// TODO MUST happen in WriteConfig
				// lay down a custom config passed via the command line
				string content_of_custom_config_file = string.Join(Environment.NewLine, File.ReadAllLines(session["MINION_CONFIGFILE"]));
				Writeln_file(session, @"C:\salt\conf", "minion", content_of_custom_config_file);
			}



			if (session["CONFIG_TYPE"] == "Default") {
				/* ----------------------------------
				 *        3 / 4
				 * ----------------------------------
				 * Overwrite the existing config if present with the default config for salt. 
				 * Default is to use the existing config if present. 
				 * If /master and/or /minion-name is passed, those values will be used to update the new default config. 
				 
Default

This setting will reset config to be the default config contained in the pkg. 
Therefore, all existing config files should be backed up
1. `minion` config renamed to `minion-<timestamp>.bak`
2. `minion_id` file renamed to `minion_id-<timestamp>.bak`
3. `minion.d` directory renamed to `minion.d-<timestamp>.bak`
Then the default config file is laid down by the installer... settings for `master` and `minion id` should be applied to the default config if passed
				 */

				// TODO SHOULD happen in WriteConfig
				Backup_configuration_files_from_previous_installation(session);

				if (session["MASTER"]    == "#") {
					session["MASTER"] = "salt";
					session.Log("...MASTER set to salt because it was unset and CONFIG_TYPE=Default");
				}
				if (session["MINION_ID"] == "#") {
					session["MINION_ID"] = Environment.MachineName;
					session.Log("...MINION_ID set to hostname because it was unset and CONFIG_TYPE=Default");
				}
      }


			if (session["CONFIG_TYPE"] == "New") {
				/* -------------------------------
				 *       4 / 4
				 * -------------------------------
				 */
				// If the msi property has value #, this is our convention for "unset"
				// This means the user has not set the value on commandline (GUI comes later)
				// If the msi property has value different from # "unset", the user has set the master
				// msi propery has precedence over kept config 
				// Only if msi propery is unset, set value of previous installation

				/////////////////master
				if (session["MASTER"] == "#") {
					session.Log("...MASTER       kept config   =" + master_from_previous_installation);
					if (master_from_previous_installation != "") {
						session["MASTER"] = master_from_previous_installation;
						session.Log("...MASTER set to kept config");
					} else {
						session["MASTER"] = "salt";
						session.Log("...MASTER set to salt because it was unset and no kept config");
					}
				}

				///////////////// minion id
				// only if MINION_ID_CACHING
				if (session["MINION_ID_CACHING"] == "1" && session["MINION_ID"] == "#") {
					session.Log("...MINION_ID   kept config   =" + id_from_previous_installation);
					if (id_from_previous_installation != "") {
						session.Log("...MINION_ID set to kept config ");
						session["MINION_ID"] = id_from_previous_installation;
					} else {
						session["MINION_ID"] = Environment.MachineName;
						session.Log("...MINION_ID set to hostname because it was unset and no previous installation and CONFIG_TYPE=New and MINION_ID_CACHING");
					}
				}
			}


			// TODO SHOULD happen in WriteConfig
			// Save the salt-master public key 
			var master_public_key_path = @"C:\salt\conf\pki\minion";  // TODO more flexible
      var master_public_key_filename = master_public_key_path + "\\" + @"minion_master.pub";
      bool MASTER_KEY_set = session["MASTER_KEY"] != "#";
      session.Log("...master key earlier config file exists = " + File.Exists(master_public_key_filename));
      session.Log("...master key msi property given         = " + MASTER_KEY_set);
      if (MASTER_KEY_set) {
        String master_key_lines = "";   // Newline after 64 characters
        int count_characters = 0;
        foreach (char character in session["MASTER_KEY"]) {
          master_key_lines += character;
          count_characters += 1;
          if (count_characters % 64 == 0) {
            master_key_lines += Environment.NewLine;
          }
        }
        string new_master_pub_key =
          "-----BEGIN PUBLIC KEY-----" + Environment.NewLine +
          master_key_lines + Environment.NewLine +
          "-----END PUBLIC KEY-----";
				Directory.CreateDirectory(master_public_key_path); 
				File.WriteAllText(master_public_key_filename, new_master_pub_key);
      }
    }


    // Leaves the Config
    [CustomAction]
    public static ActionResult del_NSIS_DECAC(Session session) {
      session.Log("...Begin del_NSIS_DECAC");
      if (!delete_NSIS_not_using_uninst_DECAC(session)) return ActionResult.Failure;
      session.Log("...End del_NSIS_DECAC");
      return ActionResult.Success;
    }

    private static bool delete_NSIS_not_using_uninst_DECAC(Session session) {
      /*
       * If NSIS is installed:
       *   remove salt-minion service, 
       *   remove registry
       *   remove files, except /salt/conf and /salt/var
       *   
			 *   I could use \salt\uninst.exe and preserve the 2 directories by moving them into safety first. 
			 *   This would be much shorter and cleaner code
      */
			session.Log("...Begin delete_NSIS_files");
      RegistryKey reg = Registry.LocalMachine;
      // ?When this is under    SOFTWARE\WoW6432Node
      string Salt_uninstall_regpath64 = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
      string Salt_uninstall_regpath32 = @"SOFTWARE\WoW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
      var SaltRegSubkey64 = reg.OpenSubKey(Salt_uninstall_regpath64);
      var SaltRegSubkey32 = reg.OpenSubKey(Salt_uninstall_regpath32);

      bool NSIS_is_installed64 = (SaltRegSubkey64 != null) && SaltRegSubkey64.GetValue("UninstallString").ToString().Equals(@"c:\salt\uninst.exe", StringComparison.OrdinalIgnoreCase);
      bool NSIS_is_installed32 = (SaltRegSubkey32 != null) && SaltRegSubkey32.GetValue("UninstallString").ToString().Equals(@"c:\salt\uninst.exe", StringComparison.OrdinalIgnoreCase);
      session.Log("delete_NSIS_files:: NSIS_is_installed64 = " + NSIS_is_installed64);
      session.Log("delete_NSIS_files:: NSIS_is_installed32 = " + NSIS_is_installed32);
      if (NSIS_is_installed64 || NSIS_is_installed32) {
        session.Log("delete_NSIS_files:: Going to stop service salt-minion ...");
        shellout(session, "sc stop salt-minion");
        session.Log("delete_NSIS_files:: Going to delete service salt-minion ...");
        shellout(session, "sc delete salt-minion"); // shellout waits, but does sc? Does this work?

        session.Log("delete_NSIS_files:: Going to delete ARP registry64 entry for salt-minion ...");
        try { reg.DeleteSubKeyTree(Salt_uninstall_regpath64); } catch (Exception ex) { just_ExceptionLog("", session, ex); }
        session.Log("delete_NSIS_files:: Going to delete ARP registry32 entry for salt-minion ...");
        try { reg.DeleteSubKeyTree(Salt_uninstall_regpath32); } catch (Exception ex) { just_ExceptionLog("", session, ex); }

        session.Log("delete_NSIS_files:: Going to delete files ...");
        try { Directory.Delete(@"c:\salt\bin", true); } catch (Exception ex) { just_ExceptionLog("", session, ex); }
        try { File.Delete(@"c:\salt\uninst.exe"); } catch (Exception ex) { just_ExceptionLog("", session, ex); }
        try { File.Delete(@"c:\salt\nssm.exe"); } catch (Exception ex) { just_ExceptionLog("", session, ex); }
        try { foreach (FileInfo fi in new DirectoryInfo(@"c:\salt").GetFiles("salt*.*")) { fi.Delete(); } } catch (Exception) {; }
      }
      session.Log("...End delete_NSIS_files");
      return true;
    }


    private static void read_master_and_id_from_file(Session session, String configfile, ref String ref_master, ref String ref_id) {
      session.Log("...searching master and id in " + configfile);
      bool configExists = File.Exists(configfile);
      session.Log("......file exists " + configExists);
      if (!configExists) { return; }
      session.Message(InstallMessage.Progress, new Record(2, 1));  // Who is reading this?
      string[] configLines = File.ReadAllLines(configfile);
        Regex r = new Regex(@"^([a-zA-Z_]+):\s*([0-9a-zA-Z_.-]+)\s*$");
        foreach (string line in configLines) {
          if (r.IsMatch(line)) {
            Match m = r.Match(line);
            string key = m.Groups[1].ToString();
            string value = m.Groups[2].ToString();
            //session.Log("...ANY KEY " + key + " " + value);
            if (key == "master") {
              ref_master = value;
              session.Log("......master " + ref_master);
            }
            if (key == "id") {
              ref_id = value;
              session.Log("......id " + ref_id);
            }
          }
        }
    }



		/*
		 * This function must leave the config files according to the CONFIG_TYPE's 1-4
		 * This function is deferred (_DECAC)
		 * This function runs after the msi has created the c:\salt\conf\minion file, which is a comment-only text.
		 * If there was a previous install, there could be many config files.
		 * The previous install c:\salt\conf\minion file could contain non-comments.
		 * One of the non-comments could be master.
		 * It could be that this installer has a different master.
		 * 
		 */
		// Must have this signature or cannot uninstall not even write to the log
		[CustomAction]
		public static ActionResult WriteConfig_DECAC(Session session) /***/ {
			string zmq_filtering = "";
			string master = "";
			string id = "";
			string minion_id_caching = "";
			string minion_id_remove_domain = "";
			bool found = false;

			session.Log(@"...WriteConfig_DECAC START");
			
			replace_in_existing_cfiles_DECAC(session, "zmq_filtering", ref zmq_filtering, ref found);
			if (!found && zmq_filtering == "True") {
				append_to_config_DECAC(session, "zmq_filtering", zmq_filtering);
			}
			replace_in_existing_cfiles_DECAC(session, "master", ref master, ref found);
			if (!found) {
				append_to_config_DECAC(session, "master", master);
			}
			replace_in_existing_cfiles_DECAC(session, "id", ref id, ref found);
			if (!found) {
				append_to_config_DECAC(session, "id", id);			
			}
			replace_in_existing_cfiles_DECAC(session, "minion_id_caching", ref minion_id_caching, ref found);
			if (!found && minion_id_caching != "1") {
				append_to_config_DECAC(session, "minion_id_caching", minion_id_caching);
			}
			replace_in_existing_cfiles_DECAC(session, "minion_id_remove_domain", ref minion_id_remove_domain, ref found);
			if (!found && minion_id_remove_domain != "") {
				append_to_config_DECAC(session, "minion_id_remove_domain", minion_id_remove_domain);
			}

			session.Log(@"...WriteConfig_DECAC STOP");
			return ActionResult.Success;
		}


		private static void replace_in_existing_cfiles_DECAC(Session session, string SaltKey, ref string CustomActionData_value, ref bool replaced) {
      session.Log("...replace_in_existing_cfiles_DECAC key " + SaltKey);

      CustomActionData_value = session.CustomActionData[SaltKey];

      session.Message(InstallMessage.Progress, new Record(2, 1));

      // pattern description
      // ^        start of line
      //          anything after the colon is ignored and would be removed 
      string pattern = "^" + SaltKey + ":";
      string replacement = String.Format(SaltKey + ": {0}", CustomActionData_value);

      // Replace in all files
      replace_pattern_in_all_config_files_DECAC(session, pattern, replacement, ref replaced);

      session.Message(InstallMessage.Progress, new Record(2, 1));
      session.Log(@"...replace_in_existing_cfiles_DECAC Value " + CustomActionData_value.ToString());
      session.Log(@"...replace_in_existing_cfiles_DECAC found_before_replacement " + replaced.ToString());
    }


		static private void append_to_config_DECAC(Session session, string key, string value) {
			string MINION_CONFIGDIR = getConfigdDirectoryLocation_DECAC(session);
			if (session.CustomActionData["CONFIG_TYPE"] == "New") {
				//CONFIG_TYPE New creates a minion.d/*.conf file
				Write_file(session, MINION_CONFIGDIR, key+".conf", key+": " + value);
			} else {	
				// Shane: CONFIG_TYPES 1-3 change only the MINION_CONFIGFILE, not the minion.d/*.conf files, because the admin knows what he is doing.
				insert_value_after_comment_or_end_in_minionconfig_file(session, key, value);
			}
		}

		static private void insert_value_after_comment_or_end_in_minionconfig_file(Session session, string key, string value) {
			string MINION_CONFIGFILE = getConfigFileLocation_DECAC(session);

			string[] configLines_in = File.ReadAllLines(MINION_CONFIGFILE);
			string[] configLines_out = new string[configLines_in.Length + 1];
			int configLines_out_index = 0;

			session.Log("...insert_value_after_comment_or_end  key  {0}", key);
			session.Log("...insert_value_after_comment_or_end  value  {0}", value);
			bool found = false;
			for (int i = 0; i < configLines_in.Length; i++) {
				configLines_out[configLines_out_index++] = configLines_in[i];
				if (!found && configLines_in[i].StartsWith("#" + key + ":")) {
					found = true;
					session.Log("...insert_value_after_comment_or_end..found the # in       {0}", configLines_in[i]);
					configLines_out[configLines_out_index++] = value;
				}
			}
			if (!found) {
				session.Log("...insert_value_after_comment_or_end..end");
				configLines_out[configLines_out_index++] = value;
			}
			File.WriteAllLines(MINION_CONFIGFILE, configLines_out);
		}

		private static void Write_file(Session session, string path, string filename, string filecontent) {
      System.IO.Directory.CreateDirectory(path);  // Ensures that the path exists
      File.WriteAllText(path + "\\" + filename, filecontent);       //  throws an Exception if path does not exist
			session.Log(@"...created " + path + "\\" + filename);
    }

		private static void Writeln_file(Session session, string path, string filename, string filecontent) {
			Write_file(session, path, filename, filecontent + Environment.NewLine);
		}



		/*
		 * "All config" files means:
		 *   conf/minion
		 *   conf/minion.d/*.conf           (only for New)
		 *
		 * MAYBE this function could input a dictionary of key/value pairs, because it reopens all config files over and over.
		 *
		 */
		private static void replace_pattern_in_all_config_files_DECAC(Session session, string pattern, string replacement, ref bool replaced) {
      string MINION_CONFIGFILE = getConfigFileLocation_DECAC(session);
			string MINION_CONFIGDIR = getConfigdDirectoryLocation_DECAC(session);

			replace_pattern_in_one_config_file_DECAC(session, MINION_CONFIGFILE, pattern, replacement, ref replaced);

			// Shane wants that the installer changes only the MINION_CONFIGFILE, not the minion.d/*.conf files
			if (session.CustomActionData["CONFIG_TYPE"] == "New") {
				// Go into the minion.d/ folder
				if (Directory.Exists(MINION_CONFIGDIR)) {
					var conf_files = System.IO.Directory.GetFiles(MINION_CONFIGDIR, "*.conf");
					foreach (var conf_file in conf_files) {
						// skip _schedule.conf
						if (conf_file.EndsWith("_schedule.conf")) { continue; }
						replace_pattern_in_one_config_file_DECAC(session, conf_file, pattern, replacement, ref replaced);
					}
				}
			}
    }

    private static void replace_pattern_in_one_config_file_DECAC(Session session, string config_file, string pattern, string replacement, ref bool replaced) {
      /*
       */
      string[] configLines = File.ReadAllLines(config_file);
      session.Message(InstallMessage.Progress, new Record(2, 1));
      session.Log("replace_pattern_in_config_file..config file    {0}", config_file);
      session.Message(InstallMessage.Progress, new Record(2, 1));
			for (int i = 0; i < configLines.Length; i++) {
				if (configLines[i].StartsWith(replacement)) {
					replaced = true;
					session.Log("replace_pattern_in_config_file..found the replacement in line        {0}", configLines[i]);
				}
				if (Regex.IsMatch(configLines[i], pattern)) {
					session.Log("replace_pattern_in_config_file..pattern        {0}", pattern);
					session.Log("replace_pattern_in_config_file..matched  line  {0}", configLines[i]);
					session.Log("replace_pattern_in_config_file..replaced line  {0}", replacement);
					configLines[i] = replacement + "\n";
					replaced = true;
				}
			}

			session.Message(InstallMessage.Progress, new Record(2, 1));
      File.WriteAllLines(config_file, configLines);
    }

		private static void shellout(Session session, string s) {
      // This is a handmade shellout routine
      session.Log("...shellout(" + s+")");
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


    private static string getConfigFileLocation_DECAC(Session session) {
			// DECAC means you must access data helper properties at session.CustomActionData[*]
			return session.CustomActionData["root_dir"] + "conf\\minion";
    }

		private static string getConfigdDirectoryLocation_DECAC(Session session) {
			// DECAC means you must access data helper properties at session.CustomActionData[*]
			return session.CustomActionData["root_dir"] + "conf\\minion.d";
		}


		private static string getConfigdDirectoryLocation_IMCAC(Session session) {
			// IMCAC means ou can directly access msi properties at session[*]
			// session["INSTALLFOLDER"] ends with a backslash, e.g. C:\salt\ 
			return session["INSTALLFOLDER"] + "conf\\minion.d";
		}

		private static void just_ExceptionLog(string description, Session session, Exception ex) {
      session.Log(" ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ");
			session.Log(description);
			session.Log("Exception: {0}", ex.Message.ToString());
      session.Log(ex.StackTrace.ToString());
    }

		
		private static void Backup_configuration_files_from_previous_installation(Session session) {
			string timestamp_bak = "-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + ".bak";
			Move_file(@"C:\salt\conf\minion", timestamp_bak);
			Move_file(@"C:\salt\conf\minion_id", timestamp_bak);
			Move_dir(@"C:\salt\conf\minion.d", timestamp_bak);
		}

		private static void Move_file(string ffn, string timestamp_bak) {
			if (File.Exists(ffn)) { File.Move(ffn, ffn + timestamp_bak); }
		}

		private static void Move_dir(string ffn, string timestamp_bak) {
			if (Directory.Exists(ffn)) { Directory.Move(ffn, ffn + timestamp_bak); }
		}
	}
}
