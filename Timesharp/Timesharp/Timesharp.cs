using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Timesharp {
	static class Timesharp {
		#region [GET Time]
		
		#region [TCP method]
		public static DateTime GetNistDtTcp(bool convertToLocalTime) {
			Random ran = new Random(DateTime.Now.Millisecond);
			DateTime date = DateTime.Today;
			string serverResponse = string.Empty;

			// Represents the list of NIST servers
			string[] servers = new string[] {
						 "64.90.182.55",
						 "206.246.118.250",
						 "207.200.81.113",
						 "128.138.188.172",
						 "64.113.32.5",
						 "64.147.116.229",
						 "64.125.78.85",
						 "128.138.188.172"
						  };

			// Try each server in random order to avoid blocked requests due to too frequent request
			for(int i = 0; i < 5; i++) {
				try {
					// Open a StreamReader to a random time server
					StreamReader reader = new StreamReader(new System.Net.Sockets.TcpClient(servers[ran.Next(0, servers.Length)], 13).GetStream());
					serverResponse = reader.ReadToEnd();
					reader.Close();

					// Check to see that the signiture is there
					if(serverResponse.Length > 47 && serverResponse.Substring(38, 9).Equals("UTC(NIST)")) {
						// Parse the date
						int jd = int.Parse(serverResponse.Substring(1, 5));
						int yr = int.Parse(serverResponse.Substring(7, 2));
						int mo = int.Parse(serverResponse.Substring(10, 2));
						int dy = int.Parse(serverResponse.Substring(13, 2));
						int hr = int.Parse(serverResponse.Substring(16, 2));
						int mm = int.Parse(serverResponse.Substring(19, 2));
						int sc = int.Parse(serverResponse.Substring(22, 2));

						if(jd > 51544)
							yr += 2000;
						else
							yr += 1999;

						date = new DateTime(yr, mo, dy, hr, mm, sc);

						// Convert it to the current timezone if desired
						if(convertToLocalTime)
							date = date.ToLocalTime();

						// Exit the loop
						break;
					}

				} catch(Exception ex) {
					/* Do Nothing...try the next server */
				}
			}

			return date;
		}
		#endregion

		#region [WEB REQUEST Method]
		public static DateTime GetNistDtWeb() {
			DateTime dateTime = DateTime.MinValue;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://nist.time.gov/actualtime.cgi?lzbc=siqm9b");
			request.Method = "GET";
			request.Accept = "text/html, application/xhtml+xml, */*";
			request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
			request.ContentType = "application/x-www-form-urlencoded";
			request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore); //No caching
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if(response.StatusCode == HttpStatusCode.OK) {
				StreamReader stream = new StreamReader(response.GetResponseStream());
				string html = stream.ReadToEnd();//<timestamp time=\"1395772696469995\" delay=\"1395772696469995\"/>
				string time = Regex.Match(html, @"(?<=\btime="")[^""]*").Value;
				double milliseconds = Convert.ToInt64(time) / 1000.0;
				dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
			}

			return dateTime;
		}
		#endregion

		#endregion

		#region [SET TIME]
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool SetLocalTime(ref SYSTEMTIME lpSystemTime);
		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		internal static extern bool SetSystemTime(ref SYSTEMTIME lpSystemTime);
		
		[StructLayout(LayoutKind.Sequential)]
		internal struct SYSTEMTIME {
			public ushort wYear;
			public ushort wMonth;
			public ushort wDayOfWeek;    // ignored for the SetLocalTime function
			public ushort wDay;
			public ushort wHour;
			public ushort wMinute;
			public ushort wSecond;
			public ushort wMilliseconds;
		}
		public static bool SetTime(DateTime dt) {
			SYSTEMTIME sysTime = new SYSTEMTIME() {
				wYear = (ushort)dt.Year,
				wMonth = (ushort)dt.Month,
				wDay = (ushort)dt.Day,
				wHour = (ushort)dt.Hour,
				wMinute = (ushort)dt.Minute,
				wSecond = (ushort)dt.Second,
				wMilliseconds = (ushort)dt.Millisecond
			};
			return SetLocalTime(ref sysTime);
		}

		#endregion
	}
}
