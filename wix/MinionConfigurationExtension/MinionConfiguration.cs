using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Microsoft.Tools.WindowsInstallerXml; //has be be in YBUILD. MUST NOT BE IN VISUAL STUDIO



namespace MinionConfigurationExtension
{
    public class MinionConfiguration : WixExtension
    {
	/* 2016-11.15  mkr
		If I set TargetFrameworkVersion to v4.0, in order to access the 32bit registry from 64bit Windows
		0) The code
	        static RegistryKey wrGetKey(string k, bool sw32) {
				return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, sw32 ? RegistryView.Registry32 : RegistryView.Registry64).OpenSubKey(k);
			}
		1) I get a warning that make no sense
		C:\windows\Microsoft.NET\Framework\v4.0.30319\Microsoft.Common.targets(983,5): warning MSB3644: The reference assemblies for framework ".
		NETFramework,Version=v4.0" were not found. To resolve this, install the SDK or Targeting Pack for this framework version or retarget your a
		pplication to a version of the framework for which you have the SDK or Targeting Pack installed. Note that assemblies will be resolved from
		the Global Assembly Cache (GAC) and will be used in place of reference assemblies. Therefore your assembly may not be correctly targeted f
		or the framework you intend. [C:\git\salt-windows-msi\wix\MinionConfigurationExtension\MinionConfigurationExtension.csproj]
		  whereas the log contains
		SFXCA: Binding to CLR version v4.0.30319

		2) This program finds the 32 bit NSIS in the 64 bit registry.
		  This is no good.

		I postpone to understand this and do not change TargetFrameworkVersion (leaving it at v2.0).
	*/

	

	        [CustomAction]
        public static ActionResult PrepareEvironmentBeforeInstallation(Session session) {
            /*
            Wix description: 
                Read the comments for PrepareEvironmentBeforeInstallation in wix/MinionMSI/Product.wxs

            C# description:
                This program shall perform:
                    If NSIS is installed:
                        remove salt-minion service, 
                        remove registry
                        remove files, except /salt/conf and /salt/var

            HISTORY
                2016-11-15  mkr service starting and stopping requires a missing C# library/reference. Instead, shellout("sc ...")
                2016-11-15  mkr read the registry for NSIS
                2016-11-13  mkr initiated, just logs the content of c:\

            */
            session.Log("MinionConfiguration.cs:: Begin PrepareEvironmentBeforeInstallation");
            peel_NSIS(session);
            readSimpleKeys_into_ini_file(session);
            return ActionResult.Success; //HACK
            session.Log("MinionConfiguration.cs:: End PrepareEvironmentBeforeInstallation");
        }

        private static ActionResult peel_NSIS(Session session) {
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
            return ActionResult.Success;
        }


        [CustomAction]
        public static ActionResult SetRootDir(Session session) {
            return SaveConfigKeyToFile("MinionRoot", "root_dir", session);
        }

        [CustomAction]
        public static ActionResult SetMaster(Session session) {
            return SaveConfigKeyToFile("MasterHostname", "master", session);
        }

        [CustomAction]
        public static ActionResult SetMinionId(Session session) {
            return SaveConfigKeyToFile("MinionHostname", "id", session);
        }

        private static ActionResult readSimpleKeys_into_ini_file(Session session) {
            /*
             * read simple keys from the config file into a ini file at a well known location:
             *   master
             *   id
             * 
             * These keys are later read by WiX components.
             * 
             * This is an immediate action?
             * Why not read into CustamAction Variables?
             * Is this installer running as 32 or 64 bit application?
             */
            session.Log("readSimpleKeys_into_ini_file Start");
            string[] configText = ConfigFileContent(session);
            List<string> iniContent = new List<string>();
            iniContent.Add("[Backup]");
            session.Message(InstallMessage.Progress, new Record(2, 1));
            try {
                Regex r = new Regex(@"^([a-zA-Z_]+):\s*([0-9a-zA-Z_.-]+)\s*$");
                foreach (string line in configText) {
                    //session.Log("readSimpleKeys_into_ini_file line " + line);
                    if (r.IsMatch(line)) {
                        Match m = r.Match(line);
                        string key = m.Groups[1].ToString();
                        string value = m.Groups[2].ToString();
                        session.Log("readSimpleKeys_into_ini_file key " + key);
                        session.Log("readSimpleKeys_into_ini_file val " + value);
                        if (key == "master") { iniContent.Add("master=" + value); }
                        if (key == "id") { iniContent.Add("id=" + value); }
                    }
                }
            } catch (Exception ex) { return False_after_ExceptionLog("Looping Regexp", session, ex); }
            session.Message(InstallMessage.Progress, new Record(2, 1));
            string iniFilePath = @"C:\windows\system32\config\systemprofile\local\SaltStack\Salt";
            // result in           c:\Windows\SysWOW64\config\systemprofile\local\SaltStack\Salt\minionConfigBackup.ini
            // because this (the WiX installer) is 32bit application
            string iniFile = iniFilePath + @"\minionConfigBackup.ini";
            try {
                shellout("mkdir " + iniFilePath);
                session.Log("readSimpleKeys_into_ini_file shellout mkdir " + iniFilePath);
                shellout(@"mkdir c:\asa123");
                session.Log(@"readSimpleKeys_into_ini_file shellout mkdir c:\asa123");
                File.WriteAllLines(iniFile, iniContent.ToArray());
                session.Log("readSimpleKeys_into_ini_file write " + iniFile);
            } catch (Exception ex) { return False_after_ExceptionLog("Writing to file", session, ex); }
            session.Log("readSimpleKeys_into_ini_file Stop");
            return ActionResult.Success;
        }




        private static ActionResult SaveConfigKeyToFile(string CustomActionDataKey, string SaltKey, Session session) {
            session.Message(InstallMessage.ActionStart, new Record("SetConfigKeyValue1 " + SaltKey, "SetConfigKeyValue2 " + SaltKey, "[1]"));
            session.Message(InstallMessage.Progress, new Record(0, 5, 0, 0));
            session.Log("Begin SaveConfigKeyToFile " + SaltKey);
            string value22;
            try {
                value22 = session.CustomActionData[CustomActionDataKey];
            } catch (Exception ex) { return False_after_ExceptionLog("Getting CustomActionData " + CustomActionDataKey, session, ex); }
            session.Message(InstallMessage.Progress, new Record(2, 1));
            ActionResult result = processConfigChange(session, "^" + SaltKey + ":", String.Format(SaltKey + ": {0}\n", value22));
            session.Message(InstallMessage.Progress, new Record(2, 1));
            session.Log("End SaveConfigKeyToFile " + SaltKey);
            return result;
        }

        private static ActionResult processConfigChange(Session session, string pattern, string replacement) {
            string config = getConfigFileLocation(session);
            string[] configText = ConfigFileContent(session);
            session.Message(InstallMessage.Progress, new Record(2, 1));
            session.Log("Config file: {0}", config);
            session.Message(InstallMessage.Progress, new Record(2, 1));
            try {
                for (int i = 0; i < configText.Length; i++) {
                    if (Regex.IsMatch(configText[i], pattern)) {
                        configText[i] = replacement;
                        session.Log("Set line: {0}", configText[i]);
                    }
                }
            } catch (Exception ex) { return False_after_ExceptionLog("Looping Regexp", session, ex); }
            session.Message(InstallMessage.Progress, new Record(2, 1));
            try {
                File.WriteAllLines(config, configText);
            } catch (Exception ex) { return False_after_ExceptionLog("Writing to file", session, ex); }
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


        private static string[] ConfigFileContent(Session session) {
            string config_file_path = getConfigFileLocation(session);
            string[] configText;
            session.Log("readConfigKeys Start");
            session.Message(InstallMessage.Progress, new Record(2, 1));
            session.Log("Config file: {0}", config_file_path);
            try {
                configText = File.ReadAllLines(config_file_path);
            } catch (Exception ex) { just_ExceptionLog("Reading from file", session, ex); throw ex; }
            return configText;
        }

        private static string getConfigFileLocation(Session session) {
            string config = "c:\\salt\\conf\\minion";
            return config; //HACK because immediate canot read CustomActionData
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
        private static ActionResult False_after_ExceptionLog(string description, Session session, Exception ex) {
            just_ExceptionLog(description, session, ex);
            return ActionResult.Failure;
        }



	

	
	
	
	

}

}
