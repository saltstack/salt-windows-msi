using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml;
using System;
using System.IO;

namespace MinionConfigurationExtension {
    public class MinionConfigurationUtilities : WixExtension {


        public static void Write_file(Session session, string path, string filename, string filecontent) {
            System.IO.Directory.CreateDirectory(path);  // Ensures that the path exists
            File.WriteAllText(path + "\\" + filename, filecontent);       //  throws an Exception if path does not exist
            session.Log(@"...created " + path + "\\" + filename);
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
            session.Log("...CustomActionData key {0}", key);
            string val = session.CustomActionData[key];
            session.Log("...CustomActionData val {0}", val);
            session.Log("...CustomActionData len {0}", val.Length);
            return val;
        }



        public static string getConfigFileLocation_DECAC(Session session) {
            // DECAC means you must access data helper properties at session.CustomActionData[*]
            return session.CustomActionData["root_dir"] + "conf\\minion";
        }


        public static string getConfigdDirectoryLocation_DECAC(Session session) {
            // DECAC means you must access data helper properties at session.CustomActionData[*]
            return session.CustomActionData["root_dir"] + "conf\\minion.d";
        }


        public static string getConfigdDirectoryLocation_IMCAC(Session session) {
            // IMCAC means ou can directly access msi properties at session[*]
            // session["INSTALLFOLDER"] ends with a backslash, e.g. C:\salt\ 
            return session["INSTALLFOLDER"] + "conf\\minion.d";
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