																												using System;
using Banking.Contract;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;

using log4net;
using Mono.Options;

namespace Banking.CLI
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// set default options and parameter arguments
			string configfile = "";
			string account = "";
			string task = "";
			string provider = "";
			string gui = "";
			bool help = false;
			bool list = false;
			bool debug = false;
			
			var p = new OptionSet () {
				{ "c=",	"configuration file to use. Default is provider.config in assembly directory",
					v => configfile = v },
				{ "g=", "gui to use, default is CGui (only for aqbanking)", v=> gui = v },
				{ "v", "increase verbosity, usefull for debugging", v=> debug = true },
				{ "p=", "provider to use, default is aqbanking", v => provider = v },
				{ "a=", "AccountIdentifier/Number to use", v => account = v },
				{ "t=",	"task that should be performed. Can be getbalance or gettransactions", v => task = v },
				{ "l", "list all available accounts", v => list = v != null },
				{ "h|?|help", "shows this help",  v => help = v != null },
			};

			try {
				// readin cmdline options
				p.Parse (args);
				if (help) {
					p.WriteOptionDescriptions (Console.Out);
					return;
				}
			
				// read in configuration or use default
				ProviderConfig conf;
				if (!string.IsNullOrEmpty (configfile))
					conf = new ProviderConfig (configfile);
				else
					conf = new ProviderConfig ();
				
				// setup logging
				log4net.Appender.ConsoleAppender appender;
				appender = new log4net.Appender.ConsoleAppender ();
				appender.Layout = new log4net.Layout.PatternLayout ("%-4timestamp %-5level %logger %M %ndc - %message%newline");
				log4net.Config.BasicConfigurator.Configure (appender);
				if (debug)
					appender.Threshold = log4net.Core.Level.Debug;
				else
					appender.Threshold = log4net.Core.Level.Warn;
					
				
				if (!string.IsNullOrEmpty (gui)) {
					conf.Settings.Remove ("Gui");
					conf.Settings.Add (new KeyValueConfigurationElement ("Gui", gui));
				}
				if (!string.IsNullOrEmpty (provider)) {
					conf.Settings.Remove ("Provider");
					conf.Settings.Add (new KeyValueConfigurationElement ("Provider", provider));
				}
				
				// init
				using (var banking = new BankingFactory().GetProvider(conf)) {
					
					// output account overview
					if (list) {
						foreach (var acc in banking.Accounts)
							acc.Print ();
						return;
					}
					// account requests (Balance, Transactions)
					
					// parameter sanitation
					IBankAccount b;
					if (string.IsNullOrEmpty (account)) {
						b = banking.Accounts.First ();
					} else
						b = banking.GetAccountByIdentifier (account);
						
					if (string.IsNullOrEmpty (task))
						throw new Exception ("Task needed, specify via -t <task>");
					
					switch (task) {
					case "gettransactions":		
						List<ITransaction > l = banking.GetTransactions (b);
						foreach (ITransaction t in l)
							t.Print ();
						return;					
					case "getbalance":
						var bal = banking.GetBalance (b);
						Console.WriteLine (bal);
						return;
					}
				}
			} catch (Exception e) {
				Console.WriteLine ("ERROR: " + e.Message);
				p.WriteOptionDescriptions (Console.Out);
				//throw e;
			} 
			return;
		}
	}
}