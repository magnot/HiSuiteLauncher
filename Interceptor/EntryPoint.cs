namespace Interceptor
{
    using System;
    using System.IO;    
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Globalization;
    using EasyHook;				//3rd hook api, add reference
    using Newtonsoft.Json.Linq; //3rd party json handler, add reference

    public class EntryPoint : IEntryPoint
    {
        private List<LocalHook> hooks;
        private RequestType lastRequestType;
        private SslReadDelegate originalSslRead;
        private SslWriteDelegate originalSslWrite;
        
        // local update json file
        private JObject updReq;
        private string updJson;
        
        // IMEI
        private string strImei;
        
        // flushing packets: log.txt to hisuite folder
        private StringBuilder sbLog = new StringBuilder();

        public EntryPoint(RemoteHooking.IContext context, string channelName)
        {
        }
        
       	// not working
        ~EntryPoint()
        {
        	//LogData();
        }
		
        // a few requests for us to handle
        private enum RequestType
        {
            Unknown,			// nothing
            Authorize,			// authorization
            Update,				// regular update
            UpdateFull,			// update full ota
            UpdateSwitch,		// update ?
            UpdateRollBack,		// rollback
            AppUpdate,			// hisuite update
            dummy,				// dummy for debug purposes
        }

        public void Run(RemoteHooking.IContext context, string channelName)
        {
#if DEBUG
            WinApi.AllocConsole();
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
#endif
			// current working folder
			try
	  		{
				string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
        		UriBuilder uri = new UriBuilder(codeBase);
        		string path = Uri.UnescapeDataString(uri.Path);
		  		Directory.SetCurrentDirectory(Path.GetDirectoryName(path));
	  		}
	  		catch (DirectoryNotFoundException e)
	  		{
		  		Console.WriteLine("The specified directory does not exist. {0}", e);
	  		}
	  
            this.LoadConfiguration();
            this.InstallHooks();
            this.WakeUpProcess();
            this.RunForever();
        }

        private void DebugLogMessage(string message, bool outgoingMessage)
        {
            var debugMessage = new StringBuilder();
            debugMessage.AppendLine("--------------------------------------------------------------------------------");
            debugMessage.AppendLine(outgoingMessage ? "[SEND]" : "[RECV]");
            debugMessage.AppendLine(message);
            debugMessage.AppendLine();

            Debug.WriteLine(debugMessage);
            
            // log file
            sbLog.Append(debugMessage);
        }
        
        private void LogData()
        {	
        	if (sbLog.Length <= 0) return;
        	
        	try
        	{
        		// log file
        		using (StreamWriter l = File.AppendText("log.txt"))
        		{	
					l.Write(sbLog.ToString());
					sbLog.Clear();
        		}
        	} catch (Exception e) {
            	Console.WriteLine("The file could not be read:");
            	Console.WriteLine(e.Message);
        	} // try
        }
	
        // pretty standard, don't touch it
        private void InstallHooks()
        {
            var sslReadAddress = LocalHook.GetProcAddress("SSLEAY32", "SSL_read");
            var sslWriteAddress = LocalHook.GetProcAddress("SSLEAY32", "SSL_write");

            this.originalSslRead = Marshal.GetDelegateForFunctionPointer<SslReadDelegate>(sslReadAddress);
            this.originalSslWrite = Marshal.GetDelegateForFunctionPointer<SslWriteDelegate>(sslWriteAddress);

            this.hooks = new List<LocalHook>
            {
                LocalHook.Create(sslReadAddress, new SslReadDelegate(this.SslReadHooked), null),
                LocalHook.Create(sslWriteAddress, new SslWriteDelegate(this.SslWriteHooked), null)
            };

            var excludedThreads = new int[] { };

            // Activate all hooks on all threads
            foreach (var hook in this.hooks)
            {
                hook.ThreadACL.SetExclusiveACL(excludedThreads);
            }
        }

        private void LoadConfiguration()
        {
            // TODO: Load from configuration file, working folder should already be set
            //this.DebugLogMessage("Load config file", false);
			// User IMEI
			try
			{
				strImei = System.IO.File.ReadAllText(@"IMEI.txt");
			}
			catch(Exception ex)
			{
				Console.WriteLine("No imei.txt file present:");
            	Console.WriteLine(ex.Message);
            	strImei = null;
			}
			
        }

        private void RunForever()
        {
            while (true)
            {
                Thread.Sleep(1000);
                // collect
                LogData();
            }
        }

        private int SslReadHooked(IntPtr ssl, IntPtr buffer, int length)
        {
            var read = this.originalSslRead(ssl, buffer, length);
            if (read <= 0)
            {
                return read;
            }

            var data = new byte[read];
            Marshal.Copy(buffer, data, 0, read);

            var message = Encoding.ASCII.GetString(data);
            this.DebugLogMessage(message, false);

            if (this.lastRequestType != RequestType.Unknown)
            {
                switch (this.lastRequestType)
                {
                	// authorization
                    case RequestType.Authorize:
                		break;
                	
					// regular update                		
                	case RequestType.AppUpdate:
                        break;

                    /*case RequestType.Update: //legacy
						// debug       
						var statusIndex = message.IndexOf("\"status\":");
                		var oriStatusValue = message.Substring(statusIndex + 10, 1);
                		this.DebugLogMessage(oriStatusValue, false);
                		
                		message = message.Replace(oriStatusValue, "0");
                		var spoofedRequestData = Encoding.ASCII.GetBytes(message.ToString());
               			Marshal.Copy(spoofedRequestData, 0, buffer, spoofedRequestData.Length);
                		this.originalSslRead(ssl, buffer, spoofedRequestData.Length);
                		return length;*/
                        //break;
                    
                    // rollback data
					case RequestType.UpdateRollBack:
                        break;
                        
                    // full update
                    case RequestType.UpdateFull:
                        // Replace with spoofed response
                        var spoofedResponse = new StringBuilder();
                        spoofedResponse.AppendLine("HTTP/1.1 200 OK");
                        spoofedResponse.AppendLine("Content-Type: application/json;charset=utf8"); // changes a lot
                        spoofedResponse.AppendLine("Date: " + DateTime.UtcNow.ToString("ddd, dd MMM yyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT");             
                        spoofedResponse.AppendLine("X-XSS-Protection: 1; mode=block");
                        spoofedResponse.AppendLine("X-frame-options: SAMEORIGIN");
                        spoofedResponse.AppendLine("X-Content-Type-Options: nosniff");
                        spoofedResponse.AppendLine("Server: elb"); //elb ou openresty
                        spoofedResponse.AppendLine("Content-Length: XREPX");
                        spoofedResponse.AppendLine("Connection: keep-alive");
                        spoofedResponse.AppendLine("");

                        // response from json file to hook
            			try 
        				{
            				using (StreamReader sr = new StreamReader("hisuite9_request_update.txt"))
            				{
                				updJson = sr.ReadToEnd();
                				updReq = JObject.Parse(updJson);  
                				updJson = updReq.ToString(Newtonsoft.Json.Formatting.None);
                        		spoofedResponse.Append(updJson);
                        		
                        		// response length
                        		spoofedResponse = spoofedResponse.Replace("XREPX", updJson.Length.ToString());
                        		
                        		// replace and flush it to hisuite
                        		this.DebugLogMessage(spoofedResponse.ToString(), false);
                        		var spoofedResponseData = Encoding.ASCII.GetBytes(spoofedResponse.ToString());
                        		Marshal.Copy(spoofedResponseData, 0, buffer, spoofedResponseData.Length);

                        		return spoofedResponseData.Length;
            				}
        				}
        				catch (Exception e) 
        				{
            				Console.WriteLine("The file could not be read:");
            				Console.WriteLine(e.Message);
            				return 0;
        				} // try
                }
            }

            return read;
        }

        private int SslWriteHooked(IntPtr ssl, IntPtr buffer, int length)
        {
            var data = new byte[length];
            Marshal.Copy(buffer, data, 0, length);
			
			var message = Encoding.ASCII.GetString(data);
            this.DebugLogMessage(message, true);

            if (message.Contains("POST /sp_ard_common/v1/authorize.action")) // chamado assim que um update é baixado
            {
                this.lastRequestType = RequestType.Authorize;

                // "IMEI" : "XXXXXXXXXXXXXXX",
                const int ImeiValueOffset = 10;
                const int ImeiLength = 15;
                
                var imeiParameterIndex = message.IndexOf("\"IMEI\" :");
                var originalImeiValue = message.Substring(imeiParameterIndex + ImeiValueOffset, ImeiLength);
                var spoofedRequest = message.Replace(originalImeiValue, strImei);
				
                // Replace with spoofed request
                var spoofedRequestData = Encoding.ASCII.GetBytes(spoofedRequest.ToString());
                Marshal.Copy(spoofedRequestData, 0, buffer, spoofedRequestData.Length);
                this.originalSslWrite(ssl, buffer, spoofedRequestData.Length);
                return spoofedRequestData.Length;
            }
            else if (message.Contains("POST /sp_ard_common/v2/Check.action?latest=true"))
            {
                Debug.WriteLine("Update Request Hijack - HiSuite 8");
                //message = message.Replace();
 				Debug.WriteLine("PackageType Switch");
 				this.lastRequestType = RequestType.Update; // legacy
 				
 				// Replace with spoofed request
                var spoofedRequestData = Encoding.ASCII.GetBytes(message.ToString());
                Marshal.Copy(spoofedRequestData, 0, buffer, spoofedRequestData.Length);
                // outuput
                this.originalSslWrite(ssl, buffer, spoofedRequestData.Length);
                return spoofedRequestData.Length;
            }						   
            else if (message.Contains("POST /sp_ard_common/v2/onestopCheck.action?latest=true")) // hisuite 9
            {
                Debug.WriteLine("Update Request Hijack - HiSuite 9");
                this.lastRequestType = RequestType.UpdateFull;
                
 				var checkPackageType = message;
 				if ( checkPackageType.Substring(checkPackageType.IndexOf("\"PackageType\" : ") + 17, 5) == "full\"" ) {
 					Debug.WriteLine("PackageType Switch Full");
 					this.lastRequestType = RequestType.UpdateFull;
 				} else if ( checkPackageType.Substring(checkPackageType.IndexOf("\"PackageType\" : ") + 17, 6) == "full_b" ) {
 					Debug.WriteLine("PackageType Switch RollBack");
 					this.lastRequestType = RequestType.UpdateRollBack;
 				}
 				
                // Replace with spoofed request
                var spoofedRequestData = Encoding.ASCII.GetBytes(message.ToString());
                Marshal.Copy(spoofedRequestData, 0, buffer, spoofedRequestData.Length);
                
                // outuput
                this.originalSslWrite(ssl, buffer, spoofedRequestData.Length);
                return spoofedRequestData.Length;
            }
            else if (message.Contains("POST /sp_dashboard_global/UrlCommand/CheckNewVersion.aspx")) // blocks hisuite update
            {
                //var spoofedBlockUpd = "";
                //var spoofedRequestData = Encoding.ASCII.GetBytes(spoofedBlockUpd.ToString());
                //Marshal.Copy(spoofedRequestData, 0, buffer, spoofedRequestData.Length);
                //this.originalSslWrite(ssl, buffer, spoofedRequestData.Length);
                //return length;
            }
            else
            {
                this.lastRequestType = RequestType.Unknown;
            }

            return this.originalSslWrite(ssl, buffer, length);
        }

        private void WakeUpProcess()
        {
            RemoteHooking.WakeUpProcess();
        }
    }
}
