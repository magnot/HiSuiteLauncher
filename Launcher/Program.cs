namespace Launcher
{
    using EasyHook;
    using System;
    using System.IO;
    using System.Diagnostics;
    using System.ComponentModel;
    using System.Configuration;

    internal class Program
    {	
    	// settings: path to HiSuite (use App.config)
        private static string hiSuitePath;
		private static string LibraryPath;
		
        private static void LaunchHiSuite()
        {
            System.Int32 targetPID = 0;
            //const string LibraryPath = "Interceptor.dll";
			
            // EasyHook
            try  
            {
            	RemoteHooking.CreateAndInject(hiSuitePath, string.Empty, 0, LibraryPath, LibraryPath, out targetPID, string.Empty);
            } 
            catch(Exception ex)
			{	
            	// The given EXE path could not be found.
				Console.WriteLine("Could not launch HiSuite:");
            	Console.WriteLine(ex.Message);
			}
        }

        private static void LoadConfiguration()
        {
            // TODO: Load from configuration file
            //hiSuitePath = @"C:\Program Files (x86)\HiSuite\HiSuite.exe";
            try  
            {  
            	// ms snippet
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);  
            }  
            catch (ConfigurationErrorsException)  
            {  
                Console.WriteLine("Error writing app settings");  
            } 
            
            // we didn't crash yet
            hiSuitePath = ConfigurationManager.AppSettings["hisuitefld"];
            LibraryPath = ConfigurationManager.AppSettings["interceptor"];
        }
        
        // killing the main process
        private static bool IsRunning()
    	{	
        	// kill current Hisuite proc if running
        	if (ConfigurationManager.AppSettings["killcurrentproc"] != "true") {
        		return false;
        	}
        		
        	// hisuite
        	string process = "HiSuite";
			try
			{
				foreach (Process proc in Process.GetProcessesByName(process))
            	{	
					// kill it before hooking
                	proc.Kill();
                	return true;
            	}
			}
			catch(Exception ex)
			{
				// smt wrong
				Console.WriteLine("Process not found:");
            	Console.WriteLine(ex.Message);
			}
						
        	return false;
    	}

        private static void Main(string[] args)
        {	
        	LoadConfiguration();
        	IsRunning();
            LaunchHiSuite();
        }
    }
}