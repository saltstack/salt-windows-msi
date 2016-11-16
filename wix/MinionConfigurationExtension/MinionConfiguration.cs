using System;
using Microsoft.Deployment.WindowsInstaller;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Win32;


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
				If NSIS is installed:
					remove service, registry and files (except /salt/conf and /salt/var)

			HISTORY
				2016-11-15  mkr peelNSIS
				2016-11-15  mkr read the registry for NSIS
				2016-11-13  mkr initiated, just logs the content of c:\

			*/
            session.Log("MinionConfiguration.cs:: Begin PrepareEvironmentBeforeInstallation");
            RegistryKey reg = Registry.LocalMachine;
            string NSIS_uninstall_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Salt Minion";
            bool NSIS_is_installed = reg.OpenSubKey(NSIS_uninstall_key) != null;
            session.Log("PrepareEvironmentBeforeInstallation:: NSIS_is_installed = " + NSIS_is_installed);
            if (NSIS_is_installed) {
                session.Log("PrepareEvironmentBeforeInstallation:: Going to stop service salt-minion ...");
                shellout("sc stop salt-minion");
                session.Log("PrepareEvironmentBeforeInstallation:: Going to delete service salt-minion ...");
                shellout("sc delete salt-minion");

                session.Log("PrepareEvironmentBeforeInstallation:: Going to delete ARM registry entry for salt-minion ...");
                try { reg.DeleteSubKeyTree(NSIS_uninstall_key);} catch (Exception) { ;}

                session.Log("PrepareEvironmentBeforeInstallation:: Going to delete files ...");
                try { Directory.Delete(@"c:\salt\bin", true); } catch (Exception) { ;}
                try { File.Delete(@"c:\salt\uninst.exe"); } catch (Exception) { ;}
                try { File.Delete(@"c:\salt\nssm.exe"); } catch (Exception) { ;}
                try { foreach (FileInfo fi in new DirectoryInfo(@"c:\salt").GetFiles("salt*.*")) { fi.Delete(); } } catch (Exception) { ;}
            }
            session.Log("MinionConfiguration.cs:: End PrepareEvironmentBeforeInstallation");
            return ActionResult.Success;
        }
        static void shellout(string s) {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/C " + s;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            //  Console.WriteLine(process.StandardOutput.ReadToEnd());
        }




        [CustomAction]
        public static ActionResult SetRootDir(Session session)
        {
            session.Message(InstallMessage.ActionStart, new Record("SetRootDir", "Configuring minion root_dir setting", "[1]"));
            session.Message(InstallMessage.Progress, new Record(0, 5, 0, 0));

            session.Log("Begin SetRootDir");
            string rootDir;
            
            try
            {
                rootDir = session.CustomActionData["MinionRoot"];
            }
            catch (Exception ex)
            {
                // missing MINION_ROOT
                session.Log("Exception: {0}", ex.Message.ToString());
                session.Log(ex.StackTrace.ToString());
                return ActionResult.Failure;
            }

            session.Message(InstallMessage.Progress, new Record(2, 1));

            bool result = processConfigChange(session,rootDir,"^root_dir:",String.Format("root_dir: {0}\n",rootDir));

            session.Message(InstallMessage.Progress, new Record(2, 1));

            session.Log("End SetRootDir");
            return result ? ActionResult.Success : ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult SetMaster(Session session)
        {
            session.Message(InstallMessage.ActionStart, new Record("SetMaster", "Configuring minion master setting", "[1]"));
            session.Message(InstallMessage.Progress, new Record(0, 5, 0, 0));
            
            session.Log("Begin SetMaster");

            string hostname;

            try
            {
                hostname = session.CustomActionData["MasterHostname"];
            }
            catch (Exception ex)
            {
                // missing MASTER_HOSTNAME
                session.Log("Exception: {0}", ex.Message.ToString());
                session.Log(ex.StackTrace.ToString());
                return ActionResult.Failure;
            }

            session.Message(InstallMessage.Progress, new Record(2, 1));

            bool result = processConfigChange(session,session.CustomActionData["MinionRoot"],"^#*master:",String.Format("master: {0}\n",hostname));

            session.Message(InstallMessage.Progress, new Record(2, 1));

            session.Log("End SetMaster");
            return result ? ActionResult.Success : ActionResult.Failure;
        }

        [CustomAction]
        public static ActionResult SetMinionId(Session session)
        {
            session.Message(InstallMessage.ActionStart, new Record("SetMinionId", "Configuring minion id setting", "[1]"));
            session.Message(InstallMessage.Progress, new Record(0, 5, 0, 0));

            session.Log("Begin SetMinionId");

            string hostname;

            try
            {
                hostname = session.CustomActionData["MinionHostname"];
            }
            catch (Exception ex)
            {
                // missing MINION_HOSTNAME
                session.Log("Exception: {0}", ex.Message.ToString());
                session.Log(ex.StackTrace.ToString());
                return ActionResult.Failure;
            }

            session.Message(InstallMessage.Progress, new Record(2, 1));

            bool result = processConfigChange(session, session.CustomActionData["MinionRoot"], "^#*id:", String.Format("id: {0}\n", hostname));

            session.Message(InstallMessage.Progress, new Record(2, 1));

            session.Log("End SetMinionId");
            return result ? ActionResult.Success : ActionResult.Failure;
        }

        private static bool processConfigChange(Session session, string root, string pattern, string replacement)
        {
            string config;
            string[] configText;

            try
            {
                config = root + "conf\\minion";
            }
            catch (Exception ex)
            {
                session.Log("Exception: {0}", ex.Message.ToString());
                session.Log(ex.StackTrace.ToString());
                return false;
            }

            session.Message(InstallMessage.Progress, new Record(2, 1));
            session.Log("Config file: {0}", config);

            try
            {
                configText = File.ReadAllLines(config);
            }
            catch (Exception ex)
            {
                session.Log("Exception: {0}", ex.Message.ToString());
                session.Log(ex.StackTrace.ToString());
                return false;
            }

            session.Message(InstallMessage.Progress, new Record(2, 1));

            try
            {
                for (int i=0; i < configText.Length; i++)
                {
                    if (Regex.IsMatch(configText[i], pattern))
                    {
                        configText[i] = replacement;
                        session.Log("Set line: {0}", configText[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                session.Log("Exception: {0}", ex.Message.ToString());
                session.Log(ex.StackTrace.ToString());
                return false;
            }

            session.Message(InstallMessage.Progress, new Record(2, 1));

            try
            {
                File.WriteAllLines(config, configText);
            }
            catch (Exception ex)
            {
                session.Log("Exception: {0}", ex.Message.ToString());
                session.Log(ex.StackTrace.ToString());
                return false;
            }

            return true;
        }
    }
}
