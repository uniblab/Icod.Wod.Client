// Icod.Wod.Client.exe executes Work on Demand (.wod) schematics.
// Copyright 2023, Timothy J. Bruce

/*
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Linq;

namespace Icod.Wod.Client {

	public static class Program {

		public const System.Int32 BufferSize = 16384;

		[System.STAThread]
		public static System.Int32 Main( System.String[] args ) {
			var processor = new Icod.Argh.Processor(
				new Icod.Argh.Definition[] {
					new Icod.Argh.Definition( "help", new System.String[] { "-h", "--help", "/help" } ),
					new Icod.Argh.Definition( "copyright", new System.String[] { "-c", "--copyright", "/copyright" } ),
				},
				System.StringComparer.OrdinalIgnoreCase
			);
			processor.Parse( args );

			var len = args.Length;
			if ( processor.Contains( "help" ) ) {
				PrintUsage();
				return 1;
			} else if ( processor.Contains( "copyright" ) ) {
				PrintCopyright();
				return 1;
			} else if ( 1 != len ) {
				PrintUsage();
				return 1;
			}

			System.Int32 output = 1;
			System.Exception? err = null;
			Icod.Wod.WorkOrder? workOrder = null;
			var filePathName = args[ 0 ];
			try {
				workOrder = GetSchematic( filePathName! );
				if ( workOrder is null ) {
					throw new System.ApplicationException( "WorkOrder null after deserialization." );
				}
				workOrder.Run();
				output = 0;
			} catch ( System.Exception e ) {
				err = e;
			}
			if ( err is not null ) {
				var msg = ParseException( filePathName, err );
				System.Console.Error.WriteLine( msg );
				SendErrorMail( msg, workOrder, System.Configuration.ConfigurationManager.AppSettings[ "defaultEmailTo" ]! );
			}

			return output;
		}

		private static void PrintUsage() {
			System.Console.Error.WriteLine( "No, no, no! Use it like this, Einstein:" );
			System.Console.Error.WriteLine( "Icod.Wod.Client.exe (-h | --help | /help)" );
			System.Console.Error.WriteLine( "Icod.Wod.Client.exe (-c | --copyright | /copyright)" );
			System.Console.Error.WriteLine( "Icod.Wod.Client.exe schematicPathName" );
			System.Console.Error.WriteLine( "Icod.Wod.Client.exe executes Work on Demand (.wod) schematics." );
			System.Console.Error.WriteLine( "schematicPathName may be a relative or absolute path." );
			System.Console.Error.WriteLine( "Example: Icod.Wod.Client.exe MyTestSchematic.xml" );
			System.Console.Error.WriteLine( "Example: Icod.Wod.Client.exe ..\\scripts\\ImportSales.xml" );
			System.Console.Error.WriteLine( "Example: Icod.Wod.Client.exe \"D:\\Scheduled Tasks\\Dailies\\Workday People.xml\"" );
		}
		private static void PrintCopyright() {
			var copy = new System.String[] {
				"Icod.Wod.Client.exe executes Work on Demand (.wod) schematics.",
				"Copyright (C) 2023 Timothy J. Bruce",
				"",
				"This program is free software: you can redistribute it and / or modify",
				"it under the terms of the GNU General Public License as published by",
				"the Free Software Foundation, either version 3 of the License, or",
				"( at your option ) any later version.",
				"",
				"This program is distributed in the hope that it will be useful,",
				"but WITHOUT ANY WARRANTY; without even the implied warranty of",
				"MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the",
				"GNU General Public License for more details.",
				"",
				"You should have received a copy of the GNU General Public License",
				"along with this program.If not, see <https://www.gnu.org/licenses/>."
			};
			foreach ( var line in copy ) {
				System.Console.WriteLine( line );
			}
		}

		private static void SendErrorMail( System.String body, Icod.Wod.WorkOrder? workOrder, System.String defaultEmailTo ) {
			using ( var msg = new System.Net.Mail.MailMessage() ) {
				var to = msg.To;
				if ( ( null == workOrder ) || System.String.IsNullOrEmpty( workOrder.EmailList ) ) {
					to.Add( defaultEmailTo );
				} else {
					to.Add( workOrder.EmailList );
					if ( 0 == to.Count ) {
						to.Add( defaultEmailTo );
					}
				}
				msg.SubjectEncoding = System.Text.Encoding.GetEncoding( "us-ascii" );
				msg.Subject = ( ( workOrder is null ) || System.String.IsNullOrEmpty( workOrder.JobName ) )
					? "Work Order error!"
					: workOrder.JobName + " Work Order error!"
				;
				msg.IsBodyHtml = false;
				msg.BodyEncoding = msg.SubjectEncoding;
				msg.Body = body;
				using ( var client = new System.Net.Mail.SmtpClient() ) {
					client.Send( msg );
				}
			}
		}

		private static Icod.Wod.WorkOrder? GetSchematic( System.String filePathName ) {
			using ( var file = new System.IO.FileStream( filePathName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, BufferSize, System.IO.FileOptions.SequentialScan ) ) {
				using ( var reader = System.Xml.XmlReader.Create( file ) ) {
					return ( new System.Xml.Serialization.XmlSerializer( typeof( Icod.Wod.WorkOrder ) ).Deserialize( reader ) as Icod.Wod.WorkOrder );
				}
			}
		}

		private static System.String ParseException( System.String filePathName, System.Exception? e ) {
			if ( e is null ) {
				throw new System.ArgumentNullException( nameof( e ) );
			}

			var output = new System.Text.StringBuilder();
			output = output.Append( "User: " );
			output = output.AppendLine( System.Environment.UserDomainName + "\\" + System.Environment.UserName );
			output = output.Append( "Host: " );
			output = output.AppendLine( System.Environment.MachineName );
			if ( !System.String.IsNullOrEmpty( filePathName ) ) {
				output = output.Append( "Schematic file: " );
				output = output.AppendLine( filePathName );
			}
			do {
				output = output.Append( "Message: " );
				output = output.AppendLine( e.Message ?? System.String.Empty );
				output = output.AppendLine( e.GetType().AssemblyQualifiedName?.ToString() );
				if ( 0 < e.Data.Keys.Count ) {
					output = output.AppendLine( "Data: " );
					foreach ( var k in e.Data.Keys ) {
						output = output.AppendFormat( "\t{0} : {1}", k, e.Data[ k ] ?? "(null)" ).AppendLine( System.String.Empty );
					}
				}
				output = output.Append( "Source: " );
				output = output.AppendLine( e.Source ?? System.String.Empty );
				if ( !System.String.IsNullOrEmpty( e.StackTrace ) ) {
					output = output.AppendLine( "Stack trace follows:" );
					output = output.AppendLine( e.StackTrace );
					output = output.AppendLine( ":End of stack trace" );
				}
				e = e.InnerException;
				if ( null != e ) {
					output = output.AppendLine();
					output = output.AppendLine( "Inner exception follows:" );
				}
			} while ( null != e );

			return output.ToString();
		}

	}

}