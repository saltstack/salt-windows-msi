using System;
using System.Collections.Generic;
using System.Text;
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

    public class Session {
        public void Log(String msg) {
            Console.WriteLine(msg);
        }
    }
    class ActionResult{
        public static ActionResult Success;
    }

    class Program {


        public static ActionResult kill_python_exe(Session session) {
            // because a running process can prevent removal of files
            // Get full path and command line from running process
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

            session.Log("Going to kill ...");
            kill_python_exe(session);

            session.Log("Going to stop service salt-minion ...");
            cutil.shellout(session, "sc stop salt-minion");

            session.Log("Going to delete service salt-minion ...");
            cutil.shellout(session, "sc delete salt-minion");

            session.Log("Going to delete ARP registry entry ...");
            cutil.del_registry_SOFTWARE_key(session, ARPstring);

            session.Log("Going to delete SOFTWARE registry entry ...");
            cutil.del_registry_SOFTWARE_key(session, SOFTWAREstring);

            session.Log("Going to delete bindir ... " + bin_dir);
            cutil.del_dir(session, bin_dir);

            session.Log("Going to delete uninst.exe ...");
            cutil.del_file(session, uninstexe);

            var bindirparent = Path.GetDirectoryName(bin_dir);
            session.Log(@"Going to delete bindir\..\salt\*.*    ...   " + bindirparent);
            if (Directory.Exists(bindirparent)) {
                try { foreach (FileInfo fi in new DirectoryInfo(bindirparent).GetFiles("salt*.*")) { fi.Delete(); } } catch (Exception) {; }
            }
            session.Log("...END del_NSIS_DECAC");
            return ActionResult.Success;
        }


        //----------------------------------------------------------------------------
        //----------------------------------------------------------------------------
        //----------------------------------------------------------------------------



        static void Main(string[] args) {
            Console.WriteLine("DebugMe!");
            Session the_session = new Session();
            del_NSIS_DECAC(the_session);
        }
    }
}

