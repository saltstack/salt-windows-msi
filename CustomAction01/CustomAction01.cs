﻿using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;  // Reference C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Management.dll
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace MinionConfigurationExtension {
    public class MinionConfiguration : WixExtension {


        [CustomAction]
        public static ActionResult ReadConfig_IMCAC(Session session) {
            /*
            When installatioin starts,there might be a previous installation.
            From the previous installation we read only two properties that we present in the installer:
              - master
              - id

            This function reads these two properties from
              - the 2 msi properties:
                - MASTER
                - MINION_ID
              - files from a provious installations:
                - the number of file the function searches depend on CONFIGURATION_TYPE
              - dependend on CONFIGURATION_TYPE, default values can be:
                - master = "salt"
                - id = %hostname%

            This function writes msi properties:
              - MASTER
              - MINION_ID

            A GUI installation can show these msi properties because this function is called before the GUI.
            */
            session.Log("...BEGIN ReadConfig_IMCAC");
            string ProgramData    = System.Environment.GetEnvironmentVariable("ProgramData");

            string ROOTDIR_old = @"C:\salt";
            string ROOTDIR_new =  Path.Combine(ProgramData, @"Salt Project\Salt");
            // Create msi proporties
            session["ROOTDIR_old"] = ROOTDIR_old;
            session["ROOTDIR_new"] = ROOTDIR_new;

            string abortReason = "";
            // Insert the first abort reason here
            if (abortReason.Length > 0) {
                session["AbortReason"] = abortReason;
            }

            session.Log("...Searching minion config file for reading master and id");
            string PREVIOUS_ROOTDIR = session["PREVIOUS_ROOTDIR"];          // From registry
            string previous_conf_config = "";
            if (PREVIOUS_ROOTDIR.Length > 0){
                previous_conf_config = PREVIOUS_ROOTDIR + @"\conf\minion";
            }
            // Search for configuration in this order: registry, new layout, old layout
            string minion_config_file = cutil.get_file_that_exist(session, new string[] {
                previous_conf_config,
                ROOTDIR_new + @"\conf\minion",
                ROOTDIR_old + @"\conf\minion"});
            string minion_config_dir = "";


            if (File.Exists(minion_config_file)) {
                string minion_dot_d_dir = minion_config_file + ".d";
                session.Log("...minion_dot_d_dir = " + minion_dot_d_dir);
                if (Directory.Exists(minion_dot_d_dir)) {
                    session.Log("... folder exists minion_dot_d_dir = " + minion_dot_d_dir);
                    DirectorySecurity dirSecurity = Directory.GetAccessControl(minion_dot_d_dir);
                    IdentityReference sid = dirSecurity.GetOwner(typeof(SecurityIdentifier));
                    session.Log("...owner of the minion config dir " + sid.Value);
                } else {
                    session.Log("... folder  does not exists minion_dot_d_dir = " + minion_dot_d_dir);
                }
            }

            // Check for existing config
            if (File.Exists(minion_config_file)) {
                minion_config_dir = Path.GetDirectoryName(minion_config_file);
                // Owner must be one of "Local System" or "Administrators"
                // It looks like the NullSoft installer sets the owner to
                // Administrators while the MIS installer sets the owner to
                // Local System. Salt only sets the owner of the `C:\salt`
                // directory when it starts and doesn't concern itself with the
                // conf directory. So we have to check for both.
                List<string> valid_sids = new List<string>();
                valid_sids.Add("S-1-5-18");      //Local System
                valid_sids.Add("S-1-5-32-544");  //Administrators

                // Get the SID for the conf directory
                FileSecurity fileSecurity = File.GetAccessControl(minion_config_dir);
                IdentityReference sid = fileSecurity.GetOwner(typeof(SecurityIdentifier));
                session.Log("...owner of the minion config file " + sid.Value);

                // Check to see if it's in the list of valid SIDs
                if (!valid_sids.Contains(sid.Value)) {
                    // If it's not in the list we don't want to use it. Do the following:
                    // - set INSECURE_CONFIG_FOUND to True
                    // - set CONFIG_TYPE to Default
                    session.Log("...Insecure config found, using default config");
                    session["INSECURE_CONFIG_FOUND"] = "True";
                    session["CONFIG_TYPE"] = "Default";
                }
            }

            // Set the default values for master and id
            String master_from_previous_installation = "";
            String id_from_previous_installation = "";
            // Read master and id from main config file (if such a file exists)
            read_master_and_id_from_file_IMCAC(session, minion_config_file, ref master_from_previous_installation, ref id_from_previous_installation);
            // Read master and id from minion.d/*.conf (if they exist)
            if (Directory.Exists(minion_config_dir)) {
                var conf_files = System.IO.Directory.GetFiles(minion_config_dir, "*.conf");
                foreach (var conf_file in conf_files) {
                    if (conf_file.Equals("_schedule.conf")) { continue; }            // skip _schedule.conf
                    read_master_and_id_from_file_IMCAC(session, conf_file, ref master_from_previous_installation, ref id_from_previous_installation);
                }
            }
            // Read id from minion_id (if it exists)
            // Assume the minion_id file next to the minion config file
            string minion_id_file = minion_config_file.Length == 0? "": minion_config_file + "_id";
            if (File.Exists(minion_id_file)) {
                session["MINION_ID_FILE_FOUND"] = "True";
                id_from_previous_installation = File.ReadAllLines(minion_id_file)[0];
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
            } else {
                /////////////////master
                if (session["MASTER"] == "") {
                    session.Log("...MASTER       kept config   =" + master_from_previous_installation);
                    if (master_from_previous_installation != "") {
                        session["MASTER"] = master_from_previous_installation;
                        session["CONFIG_FOUND"] = "True";
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

            // Save the salt-master public key
            // This assumes the install is silent.
            // Saving should only occur in WriteConfig_DECAC,
            // IMCAC is easier and no harm because there is no public master key in the installer.
            string MASTER_KEY = cutil.get_property_IMCAC(session, "MASTER_KEY");
            string ROOTDIR    = cutil.get_property_IMCAC(session, "ROOTDIR");
            string pki_minion_dir = Path.Combine(ROOTDIR, @"conf\minion.d\pki\minion");
            var master_key_file = Path.Combine(pki_minion_dir, "minion_master.pub");
            session.Log("...master_key_file           = " + master_key_file);
            bool MASTER_KEY_set = MASTER_KEY != "";
            session.Log("...master key earlier config file exists = " + File.Exists(master_key_file));
            session.Log("...master key msi property given         = " + MASTER_KEY_set);
            if (MASTER_KEY_set) {
                String master_key_lines = "";   // Newline after 64 characters
                int count_characters = 0;
                foreach (char character in MASTER_KEY) {
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
                if (!Directory.Exists(pki_minion_dir)) {
                    // The <Directory> declaration in Product.wxs does not create the folders
                    Directory.CreateDirectory(pki_minion_dir);
                }
                File.WriteAllText(master_key_file, new_master_pub_key);
            }
            session.Log("...END ReadConfig_IMCAC");
            return ActionResult.Success;
        }


        private static void write_master_and_id_to_file_DECAC(Session session, String configfile, string csv_multimasters, String id) {
            /* How to
             * read line
             * if line master, read multimaster, replace
             * if line id, replace
             * copy through line
            */
            char[] separators = new char[] { ',', ' ' };
            string[] multimasters = csv_multimasters.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            session.Log("...want to write master and id to " + configfile);
            bool configExists = File.Exists(configfile);
            session.Log("......file exists " + configExists);
            string[] configLinesINPUT = new List<string>().ToArray();
            List<string> configLinesOUTPUT = new List<string>();
            if (configExists) {
                configLinesINPUT = File.ReadAllLines(configfile);
            }
            session.Log("...found config lines count " + configLinesINPUT.Length);
            session.Log("...got master count " + multimasters.Length);
            session.Log("...got id " + id);

            Regex line_contains_key = new Regex(@"^([a-zA-Z_]+):");
            Regex line_contains_one_multimaster = new Regex(@"^\s*-\s*(.*)$");
            bool master_emitted = false;
            bool id_emitted = false;

            bool look_for_multimasters = false;
            foreach (string line in configLinesINPUT) {
                // search master and id
                if (line_contains_key.IsMatch(line)) {
                    Match m = line_contains_key.Match(line);
                    string key = m.Groups[1].ToString();
                    if (key == "master") {
                        look_for_multimasters = true;
                        continue; // next line
                    } else if (key == "id") {
                        // emit id
                        configLinesOUTPUT.Add("id: " + id);
                        id_emitted = true;
                        continue; // next line
                    } else {
                        if (!look_for_multimasters) {
                            configLinesOUTPUT.Add(line); // copy through
                            continue; // next line
                        }
                    }
                } else {
                    if (!look_for_multimasters) {
                        configLinesOUTPUT.Add(line); // copy through
                        continue; // next line
                    }
                }

                if (look_for_multimasters) {
                    // consume multimasters
                    if (line_contains_one_multimaster.IsMatch(line)) {
                        // consume another multimaster
                    } else {
                        look_for_multimasters = false;
                        // First emit master
                        if (multimasters.Length == 1) {
                            configLinesOUTPUT.Add("master: " + multimasters[0]);
                            master_emitted = true;
                        }
                        if (multimasters.Length > 1) {
                            configLinesOUTPUT.Add("master:");
                            foreach (string onemultimaster in multimasters) {
                                configLinesOUTPUT.Add("- " + onemultimaster);
                            }
                            master_emitted = true;
                        }
                        configLinesOUTPUT.Add(line); // Then copy through whatever is not one multimaster
                    }
                }
            }

            // input is read
            if (!master_emitted) {
                // put master after hash master
                Regex line_contains_hash_master = new Regex(@"^# master:");
                List<string> configLinesOUTPUT_hash_master = new List<string>();
                foreach (string output_line in configLinesOUTPUT) {
                    configLinesOUTPUT_hash_master.Add(output_line);
                    if(line_contains_hash_master.IsMatch(output_line)) {
                        if (multimasters.Length == 1) {
                            configLinesOUTPUT_hash_master.Add("master: " + multimasters[0]);
                            master_emitted = true;
                        }
                        if (multimasters.Length > 1) {
                            configLinesOUTPUT_hash_master.Add("master:");
                            foreach (string onemultimaster in multimasters) {
                                configLinesOUTPUT_hash_master.Add("- " + onemultimaster);
                            }
                            master_emitted = true;
                        }
                    }
                }
                configLinesOUTPUT = configLinesOUTPUT_hash_master;
            }
            if (!master_emitted) {
                // put master at end
                if (multimasters.Length == 1) {
                    configLinesOUTPUT.Add("master: " + multimasters[0]);
                }
                if (multimasters.Length > 1) {
                    configLinesOUTPUT.Add("master:");
                    foreach (string onemultimaster in multimasters) {
                        configLinesOUTPUT.Add("- " + onemultimaster);
                    }
                }
            }

            if (!id_emitted) {
                // put after hash
                Regex line_contains_hash_id = new Regex(@"^# id:");
                List<string> configLinesOUTPUT_hash_id = new List<string>();
                foreach (string output_line in configLinesOUTPUT) {
                    configLinesOUTPUT_hash_id.Add(output_line);
                    if (line_contains_hash_id.IsMatch(output_line)) {
                            configLinesOUTPUT_hash_id.Add("id: " + id);
                            id_emitted = true;
                    }
                }
                configLinesOUTPUT = configLinesOUTPUT_hash_id;
            }
            if (!id_emitted) {
                // put at end
                configLinesOUTPUT.Add("id: " + id);
            }


            session.Log("...writing to " + configfile);
            string output = string.Join("\r\n", configLinesOUTPUT.ToArray()) + "\r\n";
            File.WriteAllText(configfile, output);

        }




        private static void read_master_and_id_from_file_IMCAC(Session session, String configfile, ref String ref_master, ref String ref_id) {
            /* How to match multimasters *
                match `master: `MASTER*:
                if MASTER:
                  master = MASTER
                else, a list of masters may follow:
                  while match `- ` MASTER:
                    master += MASTER
            */
            session.Log("...searching master and id in " + configfile);
            bool configExists = File.Exists(configfile);
            session.Log("......file exists " + configExists);
            if (!configExists) { return; }
            string[] configLines = File.ReadAllLines(configfile);
            Regex line_key_maybe_value = new Regex(@"^([a-zA-Z_]+):\s*([0-9a-zA-Z_.-]*)\s*$");
            Regex line_listvalue = new Regex(@"^\s*-\s*(.*)$");
            bool look_for_keys_otherwise_look_for_multimasters = true;
            List<string> multimasters = new List<string>();
            foreach (string line in configLines) {
                if (look_for_keys_otherwise_look_for_multimasters && line_key_maybe_value.IsMatch(line)) {
                    Match m = line_key_maybe_value.Match(line);
                    string key = m.Groups[1].ToString();
                    string maybe_value = m.Groups[2].ToString();
                    //session.Log("...ANY KEY " + key + " " + maybe_value);
                    if (key == "master") {
                        if (maybe_value.Length > 0) {
                            ref_master = maybe_value;
                            session.Log("......master " + ref_master);
                        } else {
                            session.Log("...... now searching multimasters");
                            look_for_keys_otherwise_look_for_multimasters = false;
                        }
                    }
                    if (key == "id" && maybe_value.Length > 0) {
                        ref_id = maybe_value;
                        session.Log("......id " + ref_id);
                    }
                } else if (line_listvalue.IsMatch(line)) {
                    Match m = line_listvalue.Match(line);
                    multimasters.Add(m.Groups[1].ToString());
                } else {
                    look_for_keys_otherwise_look_for_multimasters = true;
                }
            }
            if (multimasters.Count > 0) {
                ref_master = string.Join(",", multimasters.ToArray());
                session.Log("......master " + ref_master);
            }
        }


       [CustomAction]
        public static void stop_service(Session session, string a_service) {
            // the installer cannot assess the log file unless it is released.
            session.Log("...stop_service " + a_service);
            ServiceController service = new ServiceController(a_service);
            service.Stop();
            var timeout = new TimeSpan(0, 0, 1); // seconds
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }


       [CustomAction]
        public static ActionResult kill_python_exe(Session session) {
            // because a running process can prevent removal of files
            // Get full path and command line from running process
            // see https://github.com/saltstack/salt/issues/42862
            session.Log("...BEGIN kill_python_exe");
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
                        }
                    } catch (Exception) {
                        // ignore wmiresults without these properties
                    }
                }
            }
            session.Log("...END kill_python_exe");
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
             *   The msi cannot use uninst.exe because the service would no longer start.
            */
            session.Log("...BEGIN del_NSIS_DECAC");
            RegistryKey HKLM = Registry.LocalMachine;

            string ARPstring = @"Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
            RegistryKey ARPreg = cutil.get_registry_SOFTWARE_key(session, ARPstring);
            string uninstexe = "";
            if (ARPreg != null) uninstexe = ARPreg.GetValue("UninstallString").ToString();
            session.Log("from REGISTRY uninstexe = " + uninstexe);

            string SOFTWAREstring = @"Salt Project\Salt";
            RegistryKey SOFTWAREreg = cutil.get_registry_SOFTWARE_key(session, SOFTWAREstring);
            var bin_dir = "";
            if (SOFTWAREreg != null) bin_dir = SOFTWAREreg.GetValue("bin_dir").ToString();
            session.Log("from REGISTRY bin_dir = " + bin_dir);
            if (bin_dir == "") bin_dir = @"C:\salt\bin";
            session.Log("bin_dir = " + bin_dir);

            session.Log("Going to stop service salt-minion ...");
            cutil.shellout(session, "sc stop salt-minion");

            session.Log("Going to delete service salt-minion ...");
            cutil.shellout(session, "sc delete salt-minion");

            session.Log("Going to kill ...");
            kill_python_exe(session);

            session.Log("Going to delete ARP registry entry ...");
            cutil.del_registry_SOFTWARE_key(session, ARPstring);

            session.Log("Going to delete SOFTWARE registry entry ...");
            cutil.del_registry_SOFTWARE_key(session, SOFTWAREstring);

            session.Log("Going to delete uninst.exe ...");
            cutil.del_file(session, uninstexe);

            // This deletes any file that starts with "salt" from the install_dir
            var bindirparent = Path.GetDirectoryName(bin_dir);
            session.Log(@"Going to delete bindir\..\salt\*.*    ...   " + bindirparent);
            if (Directory.Exists(bindirparent)){
                try { foreach (FileInfo fi in new DirectoryInfo(bindirparent).GetFiles("salt*.*")) { fi.Delete(); } } catch (Exception) {; }
            }

            // This deletes the bin directory
            session.Log("Going to delete bindir ... " + bin_dir);
            cutil.del_dir(session, bin_dir);

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
            string master = cutil.get_property_DECAC(session, "master");;
            string id = cutil.get_property_DECAC(session, "id");;
            string MOVE_CONF     = cutil.get_property_DECAC(session, "MOVE_CONF");
            string INSTALLDIR    = cutil.get_property_DECAC(session, "INSTALLDIR");
            string MINION_CONFIG = cutil.get_property_DECAC(session, "MINION_CONFIG");
            string CONFDIR = cutil.get_property_DECAC(session, "CONFDIR");
            string MINION_CONFIGFILE = Path.Combine(CONFDIR, "minion");
            session.Log("... MINION_CONFIGFILE {0}", MINION_CONFIGFILE);
            bool file_exists = File.Exists(MINION_CONFIGFILE);
            session.Log("...file exists {0}", file_exists);

            // Get environment variables
            string ProgramData = System.Environment.GetEnvironmentVariable("ProgramData");

            if (MINION_CONFIG.Length > 0) {
                apply_minion_config_DECAC(session, MINION_CONFIG);  // A single msi property is written to file
            } else {
                write_master_and_id_to_file_DECAC(session, MINION_CONFIGFILE, master, id); // Two msi properties are replaced inside files
                save_custom_config_file_if_config_type_demands_DECAC(session);     // Given file
            }
            session.Log("...END WriteConfig_DECAC");
            return ActionResult.Success;
        }


        [CustomAction]
        public static ActionResult MoveInsecureConfig_DECAC(Session session) {
            // This appends .insecure-yyyy-MM-ddTHH-mm-ss to an insecure config directory
            // C:\salt\conf.insecure-2021-10-01T12:23:32

            session.Log("...BEGIN MoveInsecureConf_DECAC");

            string timestamp_bak = ".insecure-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
            cutil.Move_dir(session, @"C:\salt\conf", timestamp_bak);

            session.Log("...END MoveInsecureConf_DECAC");

            return ActionResult.Success;
        }

        private static void save_custom_config_file_if_config_type_demands_DECAC(Session session) {
            session.Log("...save_custom_config_file_if_config_type_demands_DECAC");
            string config_type    = cutil.get_property_DECAC(session, "config_type");
            string custom_config1 = cutil.get_property_DECAC(session, "custom_config");
            string CONFDIR        = cutil.get_property_DECAC(session, "CONFDIR");

            string custom_config_final = "";
            if (!(config_type == "Custom" && custom_config1.Length > 0 )) {
                return;
            }
            if (File.Exists(custom_config1)) {
                session.Log("...found custom_config1 " + custom_config1);
                custom_config_final = custom_config1;
            } else {
                // try relative path
                string directory_of_the_msi = cutil.get_property_DECAC(session, "sourcedir");
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
            // lay down a custom config passed via the command line
            string content_of_custom_config_file = string.Join(Environment.NewLine, File.ReadAllLines(custom_config_final));
            cutil.Write_file(session, CONFDIR, "minion", content_of_custom_config_file);
        }

       [CustomAction]
        public static ActionResult DeleteConfig_DECAC(Session session) {
            // This removes not only config, but ROOTDIR or subfolders of ROOTDIR, depending on properties CLEAN_INSTALL and REMOVE_CONFIG
            // Called on install, upgrade and uninstall
            session.Log("...BEGIN DeleteConfig_DECAC");

            // Determine wether to delete everything and DIRS
            string CLEAN_INSTALL = cutil.get_property_DECAC(session, "CLEAN_INSTALL");
            string REMOVE_CONFIG = cutil.get_property_DECAC(session, "REMOVE_CONFIG");
            string INSTALLDIR    = cutil.get_property_DECAC(session, "INSTALLDIR");
            string bindir        = Path.Combine(INSTALLDIR, "bin");
            string ROOTDIR       = cutil.get_property_DECAC(session, "ROOTDIR");
            string ProgramData   = System.Environment.GetEnvironmentVariable("ProgramData");
            string ROOTDIR_old   = @"C:\salt";
            string ROOTDIR_new   =  Path.Combine(ProgramData, @"Salt Project\Salt");
            // The registry subkey deletes itself

            if (CLEAN_INSTALL.Length > 0) {
                session.Log("...CLEAN_INSTALL -- remove both old and new root_dirs");
                cutil.del_dir(session, ROOTDIR_old);
                cutil.del_dir(session, ROOTDIR_new);
            }

            session.Log("...deleting bindir (msi only deletes what it installed, not *.pyc)  = " + bindir);
            cutil.del_dir(session, bindir);

            if (REMOVE_CONFIG.Length > 0) {
                session.Log("...REMOVE_CONFIG -- remove the current root_dir");
                cutil.del_dir(session, ROOTDIR);
            } else {
                session.Log("...Not REMOVE_CONFIG -- remove var and srv from the current root_dir");
                cutil.del_dir(session, ROOTDIR, "var");
                cutil.del_dir(session, ROOTDIR, "srv");
            }

            session.Log("...END DeleteConfig_DECAC");
            return ActionResult.Success;
        }


       [CustomAction]
        public static ActionResult MoveConfig_DECAC(Session session) {
            // This moves the root_dir from the old location (C:\salt) to the
            // new location (%ProgramData%\Salt Project\Salt)
            session.Log("...BEGIN MoveConfig_DECAC");

            // Get %ProgramData%
            string ProgramData   = System.Environment.GetEnvironmentVariable("ProgramData");

            string RootDirOld = @"C:\salt";
            string RootDirNew = Path.Combine(ProgramData, @"Salt Project\Salt");
            string RootDirNewParent = Path.Combine(ProgramData, @"Salt Project");

            session.Log("...RootDirOld       " + RootDirOld + " exists: " + Directory.Exists(RootDirOld));
            session.Log("...RootDirNew       " + RootDirNew + " exists: " + Directory.Exists(RootDirNew));
            session.Log("...RootDirNewParent " + RootDirNewParent + " exists: " + Directory.Exists(RootDirNewParent));

            // Create parent dir if it doesn't exist
            if (! Directory.Exists(RootDirNewParent)) {
                Directory.CreateDirectory(RootDirNewParent);
            }

            // Requires that the parent directory exists
            // Requires that the NewDir does NOT exist
            Directory.Move(RootDirOld, RootDirNew);

            session.Log("...END MoveConfig_DECAC");
            return ActionResult.Success;
        }


        private static void apply_minion_config_DECAC(Session session, string MINION_CONFIG) {
            // Precondition: parameter MINION_CONFIG contains the content of the MINION_CONFIG property and is not empty
            // Remove all other config
            session.Log("...apply_minion_config_DECAC BEGIN");
            string CONFDIR      = cutil.get_property_DECAC(session, "CONFDIR");
            string MINION_D_DIR = Path.Combine(CONFDIR, "minion.d");
            // Write conf/minion
            string lines = MINION_CONFIG.Replace("^", Environment.NewLine);
            cutil.Writeln_file(session, CONFDIR, "minion", lines);
            // Remove conf/minion_id
            string minion_id = Path.Combine(CONFDIR, "minion_id");
            session.Log("...searching " + minion_id);
            if (File.Exists(minion_id)) {
                File.Delete(minion_id);
                session.Log("...deleted   " + minion_id);
            }
            // Remove conf/minion.d/*.conf
            session.Log("...searching *.conf in " + MINION_D_DIR);
            if (Directory.Exists(MINION_D_DIR)) {
                var conf_files = System.IO.Directory.GetFiles(MINION_D_DIR, "*.conf");
                foreach (var conf_file in conf_files) {
                    File.Delete(conf_file);
                    session.Log("...deleted   " + conf_file);
                }
            }
            session.Log(@"...apply_minion_config_DECAC END");
        }



        [CustomAction]
        public static ActionResult  BackupConfig_DECAC(Session session) {
            session.Log("...BackupConfig_DECAC BEGIN");
            string timestamp_bak = "-" + DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss") + ".bak";
            session.Log("...timestamp_bak = " + timestamp_bak);
            cutil.Move_file(session, @"C:\salt\conf\minion", timestamp_bak);
            cutil.Move_file(session, @"C:\salt\conf\minion_id", timestamp_bak);
            cutil.Move_dir(session, @"C:\salt\conf\minion.d", timestamp_bak);
            session.Log("...BackupConfig_DECAC END");

            return ActionResult.Success;
        }
    }
}
