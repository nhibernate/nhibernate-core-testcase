using System;
using System.Collections;
using System.Data;

using Iesi.Collections;

using NHibernate.Connection;

namespace NHibernate.Test
{
	/// <summary>
	/// This connection provider keeps a list of all open connections, it is used when testing to check that tests clean up after
	/// themselves.
	/// </summary>
	public class DebugConnectionProvider : DriverConnectionProvider
	{
		private readonly ISet connections = new ListSet();

		/// <summary>
		/// Gets a value indicating whether this object has open connections.
		/// </summary>
		public bool HasOpenConnections
		{
			get
			{
				// check to see if all connections that were at one point opened
				// have been closed through the CloseConnection
				// method
				if (this.connections.IsEmpty)
				{
					// there are no connections, either none were opened or
					// all of the closings went through CloseConnection.
					return false;
				}

				// Disposing of an ISession does not call CloseConnection (should it???)
				// so a Diposed of ISession will leave an IDbConnection in the list but
				// the IDbConnection will be closed (atleast with MsSql it works this way).
				foreach (IDbConnection conn in this.connections)
				{
					if (conn.State != ConnectionState.Closed)
					{
						return true;
					}
				}

				// all of the connections have been Disposed and were closed that way
				// or they were Closed through the CloseConnection method.
				return false;
			}
		}

		/// <summary>
		/// Closes all connections.
		/// </summary>
		public void CloseAllConnections()
		{
			while (!this.connections.IsEmpty)
			{
				IEnumerator en = this.connections.GetEnumerator();
				en.MoveNext();
				this.CloseConnection(en.Current as IDbConnection);
			}
		}

		/// <summary>
		/// Closes a connection.
		/// </summary>
		/// <param name="conn">The connection.</param>
		public override void CloseConnection(IDbConnection conn)
		{
			base.CloseConnection(conn);
			this.connections.Remove(conn);
		}

		/// <summary>
		/// Gets the connection.
		/// </summary>
		/// <returns>
		/// The connection.
		/// </returns>
		public override IDbConnection GetConnection()
		{
			try
			{
				IDbConnection connection = base.GetConnection();
				this.connections.Add(connection);
				return connection;
			}
			catch (Exception e)
			{
				throw new HibernateException("Could not open connection to: " + this.ConnectionString, e);
			}
		}
	}
}