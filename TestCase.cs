using System;
using System.Collections;
using System.Data;
using System.Reflection;

using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Engine;
using NHibernate.Hql.Ast.ANTLR;
using NHibernate.Hql.Classic;
using NHibernate.Mapping;
using NHibernate.Tool.hbm2ddl;
using NHibernate.Type;

using NUnit.Framework;

using log4net;
using log4net.Config;

namespace NHibernate.Test
{
	public abstract class TestCase
	{
		protected Configuration cfg;
		protected ISession lastOpenedSession;
		protected ISessionFactoryImplementor sessions;
		private const bool OutputDdl = false;

		private static readonly ILog log = LogManager.GetLogger(typeof(TestCase));
		private DebugConnectionProvider connectionProvider;

		static TestCase()
		{
			// Configure log4net here since configuration through an attribute doesn't always work.
			XmlConfigurator.Configure();
		}

		/// <summary>
		/// Gets the cache concurrency strategy.
		/// </summary>
		protected virtual string CacheConcurrencyStrategy
		{
			get { return "nonstrict-read-write"; }
			//get { return null; }
		}

		/// <summary>
		/// Gets the dialect.
		/// </summary>
		protected Dialect.Dialect Dialect
		{
			get { return NHibernate.Dialect.Dialect.GetDialect(this.cfg.Properties); }
		}

		/// <summary>
		/// To use in in-line test.
		/// </summary>
		protected bool IsAntlrParser
		{
			get { return this.sessions.Settings.QueryTranslatorFactory is ASTQueryTranslatorFactory; }
		}

		/// <summary>
		/// To use in in-line test
		/// </summary>
		protected bool IsClassicParser
		{
			get { return this.sessions.Settings.QueryTranslatorFactory is ClassicQueryTranslatorFactory; }
		}

		/// <summary>
		/// Mapping files used in the TestCase
		/// </summary>
		protected abstract IList Mappings { get; }

		/// <summary>
		/// Assembly to load mapping files from (default is NHibernate.DomainModel).
		/// </summary>
		protected virtual string MappingsAssembly
		{
			get { return "NHibernate.DomainModel"; }
		}

		/// <summary>
		/// Gets the Session Factory Implementor.
		/// </summary>
		protected ISessionFactoryImplementor Sfi
		{
			get { return this.sessions; }
		}

		/// <summary>
		/// Gets the test dialect.
		/// </summary>
		protected TestDialect TestDialect
		{
			get { return TestDialect.GetTestDialect(this.Dialect); }
		}

		/// <summary>
		/// Executes the Sql statement.
		/// </summary>
		/// <param name="sql">The sql statement.</param>
		/// <returns>
		/// The number of rows effected.
		/// </returns>
		public int ExecuteStatement(string sql)
		{
			if (this.cfg == null)
			{
				this.cfg = new Configuration();
			}

			using (IConnectionProvider prov = ConnectionProviderFactory.NewConnectionProvider(this.cfg.Properties))
			{
				IDbConnection conn = prov.GetConnection();

				try
				{
					using (IDbTransaction tran = conn.BeginTransaction())
					{
						using (IDbCommand comm = conn.CreateCommand())
						{
							comm.CommandText = sql;
							comm.Transaction = tran;
							comm.CommandType = CommandType.Text;
							int result = comm.ExecuteNonQuery();
							tran.Commit();
							return result;
						}
					}
				}
				finally
				{
					prov.CloseConnection(conn);
				}
			}
		}

		/// <summary>
		/// Executes the Sql statement.
		/// </summary>
		/// <param name="session">The session.</param>
		/// <param name="transaction">The transaction.</param>
		/// <param name="sql">The sql statement.</param>
		/// <returns>
		/// The number of rows effected.
		/// </returns>
		public int ExecuteStatement(ISession session, ITransaction transaction, string sql)
		{
			using (IDbCommand cmd = session.Connection.CreateCommand())
			{
				cmd.CommandText = sql;
				if (transaction != null)
				{
					transaction.Enlist(cmd);
				}
				return cmd.ExecuteNonQuery();
			}
		}

		/// <summary>
		/// Set up the test. This method is not overridable, but it calls <see cref="OnSetUp" /> which is.
		/// </summary>
		[SetUp]
		public void SetUp()
		{
			this.OnSetUp();
		}

		/// <summary>
		/// Checks that the test case cleans up after itself. This method is not overridable, but it calls <see cref="OnTearDown" />
		/// which is.
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			this.OnTearDown();

			bool wasClosed = this.CheckSessionWasClosed();
			bool wasCleaned = this.CheckDatabaseWasCleaned();
			bool wereConnectionsClosed = this.CheckConnectionsWereClosed();
			bool fail = !wasClosed || !wasCleaned || !wereConnectionsClosed;

			if (fail)
			{
				Assert.Fail(
					"Test didn't clean up after itself. session closed: " + wasClosed + " database cleaned: " + wasCleaned
					+ " connection closed: " + wereConnectionsClosed);
			}
		}

		/// <summary>
		/// Creates the tables used in this TestCase.
		/// </summary>
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			try
			{
				this.Configure();
				if (!this.AppliesTo(this.Dialect))
				{
					Assert.Ignore(this.GetType() + " does not apply to " + this.Dialect);
				}

				this.CreateSchema();
				try
				{
					this.BuildSessionFactory();
					if (!this.AppliesTo(this.sessions))
					{
						Assert.Ignore(this.GetType() + " does not apply with the current session-factory configuration");
					}
				}
				catch
				{
					this.DropSchema();
					throw;
				}
			}
			catch (Exception e)
			{
				this.Cleanup();
				log.Error("Error while setting up the test fixture", e);
				throw;
			}
		}

		/// <summary>
		/// Removes the tables used in this TestCase.
		/// </summary>
		/// <remarks>
		/// If the tables are not cleaned up sometimes SchemaExport runs into Sql errors because it can't drop tables because of the FKs.
		/// This will occur if the TestCase does not have the same hbm.xml files included as a previous one.
		/// </remarks>
		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			if (!this.AppliesTo(this.Dialect))
			{
				return;
			}

			this.DropSchema();
			this.Cleanup();
		}

		/// <summary>
		/// Adds the hbm mappings as resource.
		/// </summary>
		/// <param name="configuration">The NHibernate configuration.</param>
		protected virtual void AddMappings(Configuration configuration)
		{
			Assembly assembly = Assembly.Load(this.MappingsAssembly);

			foreach (string file in this.Mappings)
			{
				configuration.AddResource(this.MappingsAssembly + "." + file, assembly);
			}
		}

		/// <summary>
		/// Whether the TestCase applies to a dialect. Override in subclasses to prevent those tests from running against a particular
		/// dialect.
		/// </summary>
		/// <param name="dialect">The dialect.</param>
		/// <returns>
		/// true if the dialect should be tested, false if not.
		/// </returns>
		protected virtual bool AppliesTo(Dialect.Dialect dialect)
		{
			return true;
		}

		/// <summary>
		/// Whether the TestCase applies to a factory. Override in subclasses to prevent those tests from running against a particular
		/// factory.
		/// </summary>
		/// <param name="factory">The factory.</param>
		/// <returns>
		/// true if the factory should be tested, false if not.
		/// </returns>
		protected virtual bool AppliesTo(ISessionFactoryImplementor factory)
		{
			return true;
		}

		/// <summary>
		/// Applies the cache settings described by configuration.
		/// </summary>
		/// <param name="configuration">The NHibernate configuration.</param>
		protected void ApplyCacheSettings(Configuration configuration)
		{
			if (this.CacheConcurrencyStrategy == null)
			{
				return;
			}

			foreach (PersistentClass clazz in configuration.ClassMappings)
			{
				bool hasLob = false;
				foreach (Property prop in clazz.PropertyClosureIterator)
				{
					if (prop.Value.IsSimpleValue)
					{
						IType type = ((SimpleValue)prop.Value).Type;
						if (type == NHibernateUtil.BinaryBlob)
						{
							hasLob = true;
						}
					}
				}
				if (!hasLob && !clazz.IsInherited)
				{
					configuration.SetCacheConcurrencyStrategy(clazz.EntityName, this.CacheConcurrencyStrategy);
				}
			}

			foreach (Mapping.Collection coll in configuration.CollectionMappings)
			{
				configuration.SetCollectionCacheConcurrencyStrategy(coll.Role, this.CacheConcurrencyStrategy);
			}
		}

		/// <summary>
		/// Builds the session factory.
		/// </summary>
		protected virtual void BuildSessionFactory()
		{
			this.sessions = (ISessionFactoryImplementor)this.cfg.BuildSessionFactory();
			this.connectionProvider = this.sessions.ConnectionProvider as DebugConnectionProvider;
		}

		/// <summary>
		/// Configure NHibernate.
		/// </summary>
		/// <param name="configuration">The NHibernate configuration.</param>
		protected virtual void Configure(Configuration configuration)
		{
		}

		/// <summary>
		/// Creates the schema.
		/// </summary>
		protected virtual void CreateSchema()
		{
			new SchemaExport(this.cfg).Create(OutputDdl, true);
		}

		/// <summary>
		/// Executes the set up action.
		/// </summary>
		protected virtual void OnSetUp()
		{
		}

		/// <summary>
		/// Executes the tear down action.
		/// </summary>
		protected virtual void OnTearDown()
		{
		}

		/// <summary>
		/// Opens and returns the NHibernate session.
		/// </summary>
		/// <returns>
		/// An <see cref="ISession"/>.
		/// </returns>
		protected virtual ISession OpenSession()
		{
			this.lastOpenedSession = this.sessions.OpenSession();
			return this.lastOpenedSession;
		}

		/// <summary>
		/// Opens and returns the NHibernate session.
		/// </summary>
		/// <param name="sessionLocalInterceptor">The local session interceptor.</param>
		/// <returns>
		/// An <see cref="ISession"/>.
		/// </returns>
		protected virtual ISession OpenSession(IInterceptor sessionLocalInterceptor)
		{
			this.lastOpenedSession = this.sessions.OpenSession(sessionLocalInterceptor);
			return this.lastOpenedSession;
		}

		private bool CheckConnectionsWereClosed()
		{
			if (this.connectionProvider == null || !this.connectionProvider.HasOpenConnections)
			{
				return true;
			}

			log.Error("Test case didn't close all open connections, closing");
			this.connectionProvider.CloseAllConnections();
			return false;
		}

		private bool CheckDatabaseWasCleaned()
		{
			if (this.sessions.GetAllClassMetadata().Count == 0)
			{
				// Return early in the case of no mappings, also avoiding
				// a warning when executing the HQL below.
				return true;
			}

			bool empty;
			using (ISession s = this.sessions.OpenSession())
			{
				IList objects = s.CreateQuery("from System.Object o").List();
				empty = objects.Count == 0;
			}

			if (!empty)
			{
				log.Error("Test case didn't clean up the database after itself, re-creating the schema");
				this.DropSchema();
				this.CreateSchema();
			}

			return empty;
		}

		private bool CheckSessionWasClosed()
		{
			if (this.lastOpenedSession != null && this.lastOpenedSession.IsOpen)
			{
				log.Error("Test case didn't close a session, closing");
				this.lastOpenedSession.Close();
				return false;
			}

			return true;
		}

		private void Cleanup()
		{
			if (this.sessions != null)
			{
				this.sessions.Close();
			}
			this.sessions = null;
			this.connectionProvider = null;
			this.lastOpenedSession = null;
			this.cfg = null;
		}

		private void Configure()
		{
			this.cfg = new Configuration();
			if (TestConfigurationHelper.hibernateConfigFile != null)
			{
				this.cfg.Configure(TestConfigurationHelper.hibernateConfigFile);
			}

			this.AddMappings(this.cfg);

			this.Configure(this.cfg);

			this.ApplyCacheSettings(this.cfg);
		}

		private void DropSchema()
		{
			new SchemaExport(this.cfg).Drop(OutputDdl, true);
		}
	}
}