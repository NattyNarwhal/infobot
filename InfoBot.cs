using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Meebey.SmartIrc4net;

namespace IrcTools {
	public class InfoBot {
		public static IrcClient irc = new IrcClient();
		public static Dictionary<string, string> dict = new Dictionary<string, string>();
		public static Dictionary<string, string> conf = new Dictionary<string, string>();
		public static Random rng = new Random();
		
		public static void Main(string[]args) {
			// Init dict
			foreach (string l in File.ReadAllLines("infobot.txt")) {
				string[] s = l.Split(new char[]{'|'}, 2);
				dict.Add(s[0], s[1]);
			}
			Console.WriteLine("{DBG} Read cookies file");
			// Init settings
			foreach (string l in File.ReadAllLines("infobot.ini")) {
				string[] s = l.Split(new char[]{'='}, 2);
				conf.Add(s[0], s[1]);
			}
			Console.WriteLine("{DBG} Read config file");
			// Init IRC
			irc.OnRawMessage += new IrcEventHandler(OnRawMessage);
			irc.OnQueryMessage += new IrcEventHandler(OnQueryMessage);
			irc.OnChannelMessage += new IrcEventHandler(OnChannelMessage);
			irc.SupportNonRfc = true;
			irc.Connect(new string[]{conf["server"]} , Convert.ToInt32(conf["port"]));
			Console.WriteLine("{DBG} Connected to IRC");
			irc.Login(conf["nick"], conf["real"]);
			Console.WriteLine("{DBG} Logged in");
			foreach (string c in conf["channel"].Split(';')) {
				irc.RfcJoin(c);
				Console.WriteLine("{DBG} Joining " + c);
			}
			irc.Listen();
		}
		
		public static void OnQueryMessage(object sender, IrcEventArgs e) {
			try {
				Parse(false, e.Data.MessageArray, e.Data.Nick);
			}
			catch (IndexOutOfRangeException ex) {
				Console.WriteLine("{ERR} Someone drew outside the lines!");
			}
		}
		
		public static void OnRawMessage(object sender, IrcEventArgs e) {
			System.Console.WriteLine("{RAW} "+ e.Data.RawMessage);
		}
		
		public static void OnChannelMessage(object sender, IrcEventArgs e) {
			try {
				if (e.Data.Message.StartsWith(conf["trigger"])) {
					Parse(true, e.Data.MessageArray.Skip(1).ToArray(), e.Data.Channel);
				}
			}
			catch (IndexOutOfRangeException ex) {
				Console.WriteLine("{ERR} Someone drew outside the lines!");
			}
		}
		
		// functions
		public static void Parse(bool fromChannel, string[] message, string target) {
			switch (message[0]) {
				case "coin":
				case "flip":
					irc.SendMessage(SendType.Action, target, "flips a coin: " + FlipCoin());
					break;
				case "roll":
				case "dice":
					irc.SendMessage(SendType.Action, target, "rolls a die: " + rng.Next(1, 7).ToString());
					break;
				case "about":
					irc.SendMessage(SendType.Message, target, GetCookie(message[1]));
					break;
				case "tell":
					// instead of outptting to the target, we output to a new target
					// but first make a new array
					string[] param = message.Skip(2).ToArray();
					Parse(false, param, message[1]);
					break;
				case "list-cookies":
					string s = "These cookies are available: ";
					foreach (var x in dict) {
						s += (x.Key + " ");
					}
					irc.SendMessage(SendType.Message, target, s);
					break;
				default:
					break;
			}
		}
		
		public static string GetCookie(string cookie) {
			return dict.ContainsKey(cookie) ?  dict[cookie] : "Sorry, there is no item by that name!";
		}
		
		public static string FlipCoin() {
			return Convert.ToBoolean(rng.Next(2)) ? "heads" : "tails";
		}
	}
}
