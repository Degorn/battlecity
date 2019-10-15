using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WebSocket4Net;

namespace Battlecity
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var websocket = new WebSocket("ws://codenjoy.com:80/codenjoy-contest/ws?user=hsxsnhnir64osk1ku5ki&code=1208759298589485338"))
			{
				websocket.MessageReceived += Websocket_MessageReceived;
				websocket.Open();

				while (true)
				{
					websocket.Send("RIGHT, ACT");
					Thread.Sleep(1000);
					websocket.Send("TOP, ACT");
					Thread.Sleep(1000);
					websocket.Send("LEFT, ACT");
					Thread.Sleep(1000);
					websocket.Send("BOTTOM, ACT");
					Thread.Sleep(1000);
				}
			}
		}

		private static void Websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
		{
			Console.Clear();

			var regex = new Regex(@"=(.*)$");
			var match = regex.Matches(e.Message)[0].Value.Substring(1);

			var sb = new StringBuilder();
			var fieldLength = (int)Math.Sqrt(match.Length);
			for (int i = 0; i < match.Length; i += fieldLength)
			{
				sb.Append(match.Substring(i, fieldLength));
				sb.AppendLine();
			}

			Console.WriteLine($"{sb.ToString()}");
		}
	}
}
