using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Management;  // Reference C:\Windows\Microsoft.NET\Framework\v2.0.50727\System.Management.dll
using System.ServiceProcess;
using System.Text.RegularExpressions;


namespace MinionConfigurationExtension {
    public class cutil : WixExtension {
        //
        // DECAC means you must access data helper properties at session.CustomActionData[*]
        // IMCAC means ou can directly access msi properties at session[*]



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
                    cutil.just_ExceptionLog("", session, ex);
                }
            }
        }

        public static void Write_file(Session session, string path, string filename, string filecontent) {
            System.IO.Directory.CreateDirectory(path);  // Creates all directories and subdirectories in the specified path unless they already exist
            File.WriteAllText(Path.Combine(path, filename), filecontent);       //  throws an Exception if path does not exist
            session.Log(@"...Write_file " + Path.Combine(path, filename));
        }


        public static void Writeln_file(Session session, string path, string filename, string filecontent) {
            Write_file(session, path, filename, filecontent + Environment.NewLine);
        }


        public static void Move_file(Session session, string ffn, string timestamp_bak) {
            string target = ffn + timestamp_bak;
            session.Log("...Move_file?   " + ffn);

            if (File.Exists(ffn)) {
                session.Log("...Move_file!   " + ffn);
                if (File.Exists(target)) {
                    session.Log("...target exists   " + target);
                } else {
                    File.Move(ffn, target);
                }
            }
        }


        public static void Move_dir(Session session, string ffn, string timestamp_bak) {
            string target = ffn + timestamp_bak;
            session.Log("...Move_dir?   " + ffn);

            if (Directory.Exists(ffn)) {
                session.Log("...Move_dir!   " + ffn);
                if (Directory.Exists(target)) {
                    session.Log("...target exists   " + target);
                } else {
                    Directory.Move(ffn, ffn + timestamp_bak);
                }
            }
        }


        public static void movedir_fromAbs_toRel(Session session, string abs_from0, string rel_tmp_dir, bool into_safety, string safedir) {
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


        public static string get_property_DECAC(Session session, string key) {
            session.Log("   ...CustomActionData key {0}", key);
            string val = session.CustomActionData[key];
            session.Log("   ...CustomActionData val {0}", val);
            session.Log("   ...CustomActionData len {0}", val.Length);
            return val;
        }


        public static string getConfigFileLocation_DECAC(Session session) {
            return Path.Combine(session.CustomActionData["root_dir"], @"conf\minion");
        }


        public static string getConfigdDirectoryLocation_DECAC(Session session) {
            return Path.Combine(session.CustomActionData["root_dir"], @"conf\minion.d");
        }


        public static string getConfigFileLocation_IMCAC(Session session) {
            return Path.Combine(session["INSTALLFOLDER"], @"conf\minion");
        }


        public static string getConfigdDirectoryLocation_IMCAC(Session session) {
            return Path.Combine(session["INSTALLFOLDER"], @"conf\minion.d");
        }


        public static void just_ExceptionLog(string description, Session session, Exception ex) {
            session.Log(" ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ERROR ");
            session.Log(description);
            session.Log("Exception: {0}", ex.Message.ToString());
            session.Log(ex.StackTrace.ToString());
        }


        public static void shellout(Session session, string s) {
            // This is a handmade shellout routine
            session.Log("...shellout(" + s + ")");
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

    }
}
