using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.ServiceProcess;
using System.Diagnostics;
using System.Management;  // Reference C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Management.dll



namespace MinionConfigurationExtension {
    public class MinionConfiguration : WixExtension {


        [CustomAction]
        public static ActionResult ReadConfig_IMCAC(Session session) {
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
            session.Log("...BEGIN ReadConfig_IMCAC");
            String master_from_previous_installation = "";
            String id_from_previous_installation = "";
            // Read master and id from main config file
            string main_config = MinionConfigurationUtilities.getConfigFileLocation_IMCAC(session);
            read_master_and_id_from_file_IMCAC(session, main_config, ref master_from_previous_installation, ref id_from_previous_installation);
            // Read master and id from minion.d/*.conf
            string MINION_CONFIGDIR = MinionConfigurationUtilities.getConfigdDirectoryLocation_IMCAC(session);
            if (Directory.Exists(MINION_CONFIGDIR)) {
                var conf_files = System.IO.Directory.GetFiles(MINION_CONFIGDIR, "*.conf");
                foreach (var conf_file in conf_files) {
                    if (conf_file.Equals("_schedule.conf")) { continue; }            // skip _schedule.conf
                    read_master_and_id_from_file_IMCAC(session, conf_file, ref master_from_previous_installation, ref id_from_previous_installation);
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

            session.Log("...CONFIG_TYPE msi property  = " + session["CONFIG_TYPE"]);
            session.Log("...MASTER      msi property  = " + session["MASTER"]);
            session.Log("...MINION_ID   msi property  = " + session["MINION_ID"]);

            if (session["CONFIG_TYPE"] == "Default") {
                /* Overwrite the existing config if present with the default config for salt.
                 */

                if (session["MASTER"] == "") {
                    session["MASTER"] = "salt";
                    session.Log("...MASTER set to salt because it was unset and CONFIG_TYPE=Default");
                }
                if (session["MINION_ID"] == "") {
                    session["MINION_ID"] = Environment.MachineName;
                    session.Log("...MINION_ID set to hostname because it was unset and CONFIG_TYPE=Default");
                }

                // Would be more logical in WriteConfig, but here is easier and no harm
                Backup_configuration_files_from_previous_installation(session);

            } else {
                /* If the msi property has value #, this is our convention for "unset"
                 * This means the user has not set the value on commandline (GUI comes later)
                 * If the msi property has value different from # "unset", the user has set the master
                 * msi propery has precedence over kept config
                 * Only if msi propery is unset, set value of previous installation
                 */

                /////////////////master
                if (session["MASTER"] == "") {
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
                if (session["MINION_ID"] == "") {
                    session.Log("...MINION_ID   kept config   =" + id_from_previous_installation);
                    if (id_from_previous_installation != "") {
                        session.Log("...MINION_ID set to kept config ");
                        session["MINION_ID"] = id_from_previous_installation;
                    } else {
                        session["MINION_ID"] = Environment.MachineName;
                        session.Log("...MINION_ID set to hostname because it was unset and no previous installation and CONFIG_TYPE!=Default");
                    }
                }
            }

            // Would be more logical in WriteConfig, but here is easier and no harm because there is no public master key in the installer.
            // Save the salt-master public key
            session.Log("...SALT_CONF_PKI_MINION_FOLDER           = " + session["SALT_CONF_PKI_MINION_FOLDER"]);
            var master_public_key_filename = Path.Combine(session["SALT_CONF_PKI_MINION_FOLDER"], "minion_master.pub");
            bool MASTER_KEY_set = session["MASTER_KEY"] != "";
            session.Log("...master key earlier config file exists = " + File.Exists(master_public_key_filename));
            session.Log("...master key msi property given         = " + MASTER_KEY_set);
            session.Log("...master key msi MASTER_KEY             = " + session["MASTER_KEY"]);
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
                if (!Directory.Exists(session["SALT_CONF_PKI_MINION_FOLDER"])) {
                    // The <Directory> declaration in Product.wxs does not create the folders
                    Directory.CreateDirectory(session["SALT_CONF_PKI_MINION_FOLDER"]);
                }
                File.WriteAllText(master_public_key_filename, new_master_pub_key);
            }
            session.Log("...END ReadConfig_IMCAC");
            return ActionResult.Success;
        }


        private static void read_master_and_id_from_file_IMCAC(Session session, String configfile, ref String ref_master, ref String ref_id) {
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

        public static string get_reg(Session session, string regpath, string regkey) {
            RegistryKey hklm = Registry.LocalMachine;
            var sub_hive = hklm.OpenSubKey(regpath);
            string regval = "";
            if (sub_hive != null) {
                regval = sub_hive.GetValue(regkey).ToString();
                session.Log("...get_reg " + regkey);
                session.Log("...get_reg " + regval);
            }
            return regval;
        }


        public static string get_reg_SOFTWARE(Session session, string regpath, string regkey) {
            // search 64bit and 32bit registry
            string reg_val = get_reg(session, @"SOFTWARE\" + regpath, regkey);
            if (reg_val.Length == 0) {
                // if found nothing search 32bit registry
                reg_val   = get_reg(session, @"SOFTWARE\WoW6432Node\" + regpath, regkey);
            }
            return reg_val;
        }


        public static void set_reg(Session session, string regpath, string regkey, string regval) {
            RegistryKey hklm = Registry.LocalMachine;
            var sub_hive = hklm.CreateSubKey(regpath);
            sub_hive.SetValue(regkey, regval);
        }

        public static void del_reg(Session session, string regpath) {
            RegistryKey hklm = Registry.LocalMachine;
            var sub_hive = hklm.OpenSubKey(regpath);
            if (sub_hive != null) {
                session.Log("...del_reg " + regpath);
                hklm.DeleteSubKeyTree(regpath);
                session.Log("...del_reg " + regpath);
            }
        }

        public static void del_dir(Session session, string a_dir, string sub_dir) {
            string abs_path = a_dir;
            if (sub_dir.Length > 0) {
                abs_path = a_dir + @"\" + sub_dir;
            }
            if (a_dir.Length>0 && Directory.Exists(a_dir) && Directory.Exists(abs_path)) {
                try {
                    session.Log("...del_dir " + abs_path);
                    Directory.Delete(abs_path, true);
                } catch (Exception ex) {
                    MinionConfigurationUtilities.just_ExceptionLog("", session, ex);
                }
            }
        }

       [CustomAction]
        public static void stop_service(Session session, string a_service) {
            // because the installer cannot assess the log file
            session.Log("...stop_service " + a_service);
            ServiceController service = new ServiceController(a_service);
            service.Stop();
            var timeout = new TimeSpan(0, 0, 2); // seconds
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }

        public static void kill_python_exe(Session session) {
            // because...
            // Get full path and command line from running process
            using (var wmi_searcher = new ManagementObjectSearcher
                ("SELECT ProcessID, ExecutablePath, CommandLine FROM Win32_Process WHERE Name = 'python.exe'")) {
                foreach (ManagementObject wmi_obj in wmi_searcher.Get()) {
                    try {
                        String ProcessID = wmi_obj["ProcessID"].ToString();
                        Int32 pid = Int32.Parse(ProcessID);
                        String ExecutablePath = wmi_obj["ExecutablePath"].ToString();
                        String CommandLine = wmi_obj["CommandLine"].ToString();
                        if (CommandLine.ToLower().Contains("salt") || ExecutablePath.ToLower().Contains("salt")) {
                            session.Log("...kill_python_exe " + ExecutablePath + " " + CommandLine);
                            Process proc11 = Process.GetProcessById(pid);
                            proc11.Kill();
                            System.Threading.Thread.Sleep(10);
                        }
                    } catch (Exception) {
                        // ignore results without these properties
                    }
                }
            }
        }

       [CustomAction]
        public static ActionResult DeleteConfig_DECAC(Session session) {
            session.Log("...BEGIN DeleteConfig_DECAC");
            kill_python_exe(session);

            // Determine to delete all
            string REMOVE_CONFIG_prop = MinionConfigurationUtilities.get_property_DECAC(session, "REMOVE_CONFIG");
            string REMOVE_CONFIG_reg = get_reg_SOFTWARE(session, @"Salt Project\Salt", "REMOVE_CONFIG");
            bool DELETE_ALL = REMOVE_CONFIG_prop == "1" || REMOVE_CONFIG_reg == "1";

            // Determine and delete install_dir
            string install_dir = get_reg_SOFTWARE(session, @"Salt Project\Salt", "install_dir");
            if (DELETE_ALL) {
                del_dir(session, install_dir, "");
            } else {
                del_dir(session, install_dir, "bin");
            }

            // Determine and delete root_dir
            string root_dir = get_reg_SOFTWARE(session, @"Salt Project\Salt", "root_dir");
            if (DELETE_ALL) {
                del_dir(session, root_dir, "");
            } else {
                del_dir(session, root_dir, "var");
                del_dir(session, root_dir, "srv");
            }

            // Delete registry subkey
            if (DELETE_ALL) {
                del_reg(session, @"SOFTWARE\Salt Project\Salt");
                del_reg(session, @"SOFTWARE\WoW6432Node\Salt Project\Salt");
            }

            session.Log("...END DeleteConfig_DECAC");
            return ActionResult.Success;
        }


        [CustomAction]
        public static ActionResult del_NSIS_DECAC(Session session) {
            // Leaves the Config
            /*
             * If NSIS is installed:
             *   remove salt-minion service,
             *   remove registry
             *   remove files, except /salt/conf and /salt/var
             *
             *   Instead of the above, we cannot use uninst.exe because the service would no longer start.
            */
            session.Log("...BEGIN del_NSIS_DECAC");
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
                MinionConfigurationUtilities.shellout(session, "sc stop salt-minion");
                session.Log("delete_NSIS_files:: Going to delete service salt-minion ...");
                MinionConfigurationUtilities.shellout(session, "sc delete salt-minion"); // shellout waits, but does sc? Does this work?

                session.Log("delete_NSIS_files:: Going to delete ARP registry64 entry for salt-minion ...");
                try { reg.DeleteSubKeyTree(Salt_uninstall_regpath64); } catch (Exception ex) { MinionConfigurationUtilities.just_ExceptionLog("", session, ex); }
                session.Log("delete_NSIS_files:: Going to delete ARP registry32 entry for salt-minion ...");
                try { reg.DeleteSubKeyTree(Salt_uninstall_regpath32); } catch (Exception ex) { MinionConfigurationUtilities.just_ExceptionLog("", session, ex); }

                session.Log("delete_NSIS_files:: Going to delete files ...");
                try { Directory.Delete(@"c:\salt\bin", true); } catch (Exception ex) { MinionConfigurationUtilities.just_ExceptionLog("", session, ex); }
                try { File.Delete(@"c:\salt\uninst.exe"); } catch (Exception ex) { MinionConfigurationUtilities.just_ExceptionLog("", session, ex); }
                try { File.Delete(@"c:\salt\nssm.exe"); } catch (Exception ex) { MinionConfigurationUtilities.just_ExceptionLog("", session, ex); }
                try { foreach (FileInfo fi in new DirectoryInfo(@"c:\salt").GetFiles("salt*.*")) { fi.Delete(); } } catch (Exception) {; }
            }
            session.Log("...END del_NSIS_DECAC");
            return ActionResult.Success;
        }


        [CustomAction]
        public static ActionResult WriteConfig_DECAC(Session session) {
            /*
             * This function must leave the config files according to the CONFIG_TYPE's 1-3
             * This function is deferred (_DECAC)
             * This function runs after the msi has created the c:\salt\conf\minion file, which is a comment-only text.
             * If there was a previous install, there could be many config files.
             * The previous install c:\salt\conf\minion file could contain non-comments.
             * One of the non-comments could be master.
             * It could be that this installer has a different master.
             *
             */
            // Must have this signature or cannot uninstall not even write to the log
            session.Log("...BEGIN WriteConfig_DECAC");
            // Get msi properties
            string MOVE_CONF_PROGRAMDATA = MinionConfigurationUtilities.get_property_DECAC(session, "MOVE_CONF_PROGRAMDATA");
            string INSTALLFOLDER = MinionConfigurationUtilities.get_property_DECAC(session, "root_dir"); // TODO use same names
            string minion_config = MinionConfigurationUtilities.get_property_DECAC(session, "minion_config");
            // Get environment variables
            string ProgramData = System.Environment.GetEnvironmentVariable("ProgramData");
            // Write to registry
            set_reg(session, @"SOFTWARE\Salt Project\Salt", "install_dir", INSTALLFOLDER);
            if (MOVE_CONF_PROGRAMDATA == "1" || minion_config.Length > 0) {
                set_reg(session, @"SOFTWARE\Salt Project\Salt", "root_dir", ProgramData + @"\" + @"Salt Project\Salt");
                set_reg(session, @"SOFTWARE\Salt Project\Salt", "REMOVE_CONFIG", "1");
            } else {
                set_reg(session, @"SOFTWARE\Salt Project\Salt", "root_dir", @"C:\Salt");
            }
            // Write, move or delete files
            if (minion_config.Length > 0) {
                apply_minion_config_DECAC(session, minion_config);
            } else {
                string master = "";
                string id = "";
                if (!replace_Saltkey_in_previous_configuration_DECAC(session, "master", ref master)) {
                    append_to_config_DECAC(session, "master", master);
                }
                if (!replace_Saltkey_in_previous_configuration_DECAC(session, "id", ref id)) {
                    append_to_config_DECAC(session, "id", id);
                }
                save_custom_config_file_if_config_type_demands_DECAC(session);
            }
            session.Log("...END WriteConfig_DECAC");
            return ActionResult.Success;
        }


        private static void save_custom_config_file_if_config_type_demands_DECAC(Session session) {
            session.Log("...save_custom_config_file_if_config_type_demands_DECAC");
            string custom_config1 = session.CustomActionData["custom_config"];
            string custom_config_final = "";
            if (!(session.CustomActionData["config_type"] == "Custom" && custom_config1.Length > 0 )) {
                return;
            }
            if (File.Exists(custom_config1)) {
                session.Log("...found custom_config1 " + custom_config1);
                custom_config_final = custom_config1;
            } else {
                // try relative path
                string directory_of_the_msi = session.CustomActionData["sourcedir"];
                string custom_config2 = Path.Combine(directory_of_the_msi, custom_config1);
                if (File.Exists(custom_config2)) {
                    session.Log("...found custom_config2 " + custom_config2);
                    custom_config_final = custom_config2;
                } else {
                    session.Log("...no custom_config1 " + custom_config1);
                    session.Log("...no custom_config2 " + custom_config2);
                    return;
                }
            }
            Backup_configuration_files_from_previous_installation(session);
            // lay down a custom config passed via the command line
            string content_of_custom_config_file = string.Join(Environment.NewLine, File.ReadAllLines(custom_config_final));
            MinionConfigurationUtilities.Write_file(session, @"C:\salt\conf", "minion", content_of_custom_config_file);
        }


        private static void apply_minion_config_DECAC(Session session, string minion_config) {
            // Precondition: parameter minion_config contains the content of the MINION_CONFI property and is not empty
            // Remove all other config
            session.Log("...apply_minion_config_DECAC BEGIN");
            string conffolder           = MinionConfigurationUtilities.get_property_DECAC(session, "conffolder");
            string minion_d_conf_folder = MinionConfigurationUtilities.get_property_DECAC(session, "minion_d_conf_folder");
            // Write conf/minion
            string lines = minion_config.Replace("^", Environment.NewLine);
            MinionConfigurationUtilities.Writeln_file(session, conffolder, "minion", lines);
            // Remove conf/minion_id
            string minion_id = Path.Combine(conffolder, "minion_id");
            session.Log("...searching " + minion_id);
            if (File.Exists(minion_id)) {
                File.Delete(minion_id);
                session.Log("...deleted   " + minion_id);
            }
            // Remove conf/minion.d/*.conf
            session.Log("...searching *.conf in " + minion_d_conf_folder);
            if (Directory.Exists(minion_d_conf_folder)) {
                var conf_files = System.IO.Directory.GetFiles(minion_d_conf_folder, "*.conf");
                foreach (var conf_file in conf_files) {
                    File.Delete(conf_file);
                    session.Log("...deleted   " + conf_file);
                }
            }
            session.Log(@"...apply_minion_config_DECAC END");
        }


        private static bool replace_Saltkey_in_previous_configuration_DECAC(Session session, string SaltKey, ref string CustomActionData_value) {
            // Read SaltKey properties and convert some from 1 to True or to False
            bool replaced = false;

            session.Log("...replace_Saltkey_in_previous_configuration_DECAC Key   " + SaltKey);
            CustomActionData_value = MinionConfigurationUtilities.get_property_DECAC(session, SaltKey);

            session.Message(InstallMessage.Progress, new Record(2, 1));
            // pattern description
            // ^        start of line
            //          anything after the colon is ignored and would be removed
            string pattern = "^" + SaltKey + ":";
            string replacement = String.Format(SaltKey + ": {0}", CustomActionData_value);

            // Replace in config file
            replaced = replace_pattern_in_config_file_DECAC(session, pattern, replacement);

            session.Message(InstallMessage.Progress, new Record(2, 1));
            session.Log(@"...replace_Saltkey_in_previous_configuration_DECAC found or replaces " + replaced.ToString());
            return replaced;
        }


        private static bool replace_pattern_in_config_file_DECAC(Session session, string pattern, string replacement) {
            /*
             * config file means: conf/minion
             */
            bool replaced_in_any_file = false;
            string MINION_CONFIGFILE = MinionConfigurationUtilities.getConfigFileLocation_DECAC(session);

            replaced_in_any_file |= replace_in_file_DECAC(session, MINION_CONFIGFILE, pattern, replacement);

            return replaced_in_any_file;
        }


        static private void append_to_config_DECAC(Session session, string key, string value) {
            string MINION_CONFIGDIR = MinionConfigurationUtilities.getConfigdDirectoryLocation_DECAC(session);
            insert_value_after_comment_or_end_in_minionconfig_file(session, key, value);
        }


        static private void insert_value_after_comment_or_end_in_minionconfig_file(Session session, string key, string value) {
            string MINION_CONFIGFILE = MinionConfigurationUtilities.getConfigFileLocation_DECAC(session);
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
                    configLines_out[configLines_out_index++] = key + ": " + value;
                }
            }
            if (!found) {
                session.Log("...insert_value_after_comment_or_end..end");
                configLines_out[configLines_out_index++] = key + ": " + value;
            }
            File.WriteAllLines(MINION_CONFIGFILE, configLines_out);
        }


        private static bool replace_in_file_DECAC(Session session, string config_file, string pattern, string replacement) {
            bool replaced = false;
            bool found = false;
            session.Log("...replace_in_file_DECAC   config file    {0}", config_file);
            string[] configLines = File.ReadAllLines(config_file);
            session.Log("...replace_in_file_DECAC   lines          {0}", configLines.Length);

            for (int i = 0; i < configLines.Length; i++) {
                if (configLines[i].Equals(replacement)) {
                    found = true;
                    session.Log("...found the replacement in line        {0}", configLines[i]);
                }
                if (Regex.IsMatch(configLines[i], pattern)) {
                    session.Log("...matched  line  {0}", configLines[i]);
                    configLines[i] = replacement;
                    replaced = true;
                }
            }
            session.Log("...replace_in_file_DECAC   found          {0}", found);
            session.Log("...replace_in_file_DECAC   replaced       {0}", replaced);
            if (replaced) {
                File.WriteAllLines(config_file, configLines);
            }
            return replaced || found;
        }


        private static void Backup_configuration_files_from_previous_installation(Session session) {
            session.Log("...Backup_configuration_files_from_previous_installation");
            string timestamp_bak = "-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + ".bak";
            session.Log("...timestamp_bak = " + timestamp_bak);
            MinionConfigurationUtilities.Move_file(session, @"C:\salt\conf\minion", timestamp_bak);
            MinionConfigurationUtilities.Move_file(session, @"C:\salt\conf\minion_id", timestamp_bak);
            MinionConfigurationUtilities.Move_dir(session, @"C:\salt\conf\minion.d", timestamp_bak);
        }
    }
}
