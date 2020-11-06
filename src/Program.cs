// Icod.Wod.Client.exe : executes a specified Icod Work on Demand, or WoD, schematic file
// Copyright 2020, Timothy J. Bruce
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
			System.Int32 output = 1;
			if ( ( null == args ) || ( 1 != args.Length ) ) {
				System.Console.Error.WriteLine( "No, no, no! Use it like this, Einstein:" );
				System.Console.Error.WriteLine( "Icod.Wod.Client.exe schematicPathName" );
				System.Console.Error.WriteLine( "Example: Icod.Wod.Client.exe MyTestSchematic.xml" );
				System.Console.Error.WriteLine( "Exameple: Icod.Wod.Client.exe ..\\scripts\\ImportSales.xml" );
				System.Console.Error.WriteLine( "Example: Icod.Wod.Client.exe \"D:\\Scheduled Tasks\\Dailies\\Workday People.xml\"" );
				return output;
			}

			System.Exception err = null;
			Icod.Wod.WorkOrder workOrder = null;
			var filePathName = args[ 0 ];
			try {
				workOrder = GetSchematic( filePathName );
				if ( null == workOrder ) {
					throw new System.ApplicationException( "WorkOrder null after deserialization." );
				}
				workOrder.Run();
				output = 0;
			} catch ( System.Exception e ) {
				err = e;
			}
			if ( null != err ) {
				var msg = ParseException( filePathName, err );
				System.Console.Error.WriteLine( msg );
				SendErrorMail( msg, workOrder );
			}

			return output;
		}

		private static void SendErrorMail( System.String body, Icod.Wod.WorkOrder workOrder ) {
			using ( var msg = new System.Net.Mail.MailMessage() ) {
				var defaultEmailTo = System.Configuration.ConfigurationManager.AppSettings[ "defaultEmailTo" ];
				var to = msg.To;
				if ( ( null == workOrder ) || System.String.IsNullOrEmpty( workOrder.EmailList ) ) {
					to.Add( defaultEmailTo );
				} else {
					to.Add( workOrder.EmailList );
				}
				if ( 0 == to.Count ) {
					to.Add( defaultEmailTo );
					if ( 0 == to.Count ) {
						throw new System.InvalidOperationException( "Error email To address may not be empty or blank." );
					}
				}
				msg.SubjectEncoding = System.Text.Encoding.GetEncoding( "us-ascii" );
				msg.Subject = ( ( null == workOrder ) || System.String.IsNullOrEmpty( workOrder.JobName ) )
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

		private static Icod.Wod.WorkOrder GetSchematic( System.String filePathName ) {
			using ( var file = new System.IO.FileStream( filePathName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, BufferSize, System.IO.FileOptions.SequentialScan ) ) {
				using ( var reader = System.Xml.XmlReader.Create( file ) ) {
					return ( new System.Xml.Serialization.XmlSerializer( typeof( Icod.Wod.WorkOrder ) ).Deserialize( reader ) as Icod.Wod.WorkOrder );
				}
			}
		}

		private static System.String ParseException( System.String filePathName, System.Exception e ) {
			if ( null == e ) {
				throw new System.ArgumentNullException( "e" );
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
				output = output.AppendLine( e.GetType().AssemblyQualifiedName.ToString() );
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