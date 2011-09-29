using System;

using NHibernate.SqlTypes;

namespace NHibernate.Test
{
	/// <summary>
	/// Like NHibernate's Dialect class, but for differences only important during testing. Defaults to true for all support.  Users
	/// of different dialects can turn support off if the unit tests fail.
	/// </summary>
	public class TestDialect
	{
		private readonly Dialect.Dialect dialect;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="dialect">The dialect.</param>
		public TestDialect(Dialect.Dialect dialect)
		{
			this.dialect = dialect;
		}

		/// <summary>
		/// Gets a value indicating whether this object has broken decimal type.
		/// </summary>
		public virtual bool HasBrokenDecimalType
		{
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether this object ignores trailing whitespace.
		/// </summary>
		public virtual bool IgnoresTrailingWhitespace
		{
			get { return false; }
		}

		/// <summary>
		/// Whether two transactions can be run at the same time.  For example, with SQLite the database is locked when one transaction
		/// is run, so running a second transaction will cause a "database is locked" error message.
		/// </summary>
		public virtual bool SupportsConcurrentTransactions
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether distributed transactions are supported. Requires the MSDTC service to be started if true.
		/// </summary>
		public virtual bool SupportsDistributedTransactions
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether full joins are supported.
		/// </summary>
		public virtual bool SupportsFullJoin
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether having without group by is supported.
		/// </summary>
		public virtual bool SupportsHavingWithoutGroupBy
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether locate is supported.
		/// </summary>
		public virtual bool SupportsLocate
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether null characters in utf strings is supported.
		/// </summary>
		public virtual bool SupportsNullCharactersInUtfStrings
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether the all operator is supported.
		/// </summary>
		public virtual bool SupportsOperatorAll
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether the some operator is supported.
		/// </summary>
		public virtual bool SupportsOperatorSome
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether select for update on outer join is supported.
		/// </summary>
		public virtual bool SupportsSelectForUpdateOnOuterJoin
		{
			get { return true; }
		}

		/// <summary>
		/// Gets a test dialect.
		/// </summary>
		/// <param name="dialect">The dialect.</param>
		/// <returns>
		/// The test dialect.
		/// </returns>
		public static TestDialect GetTestDialect(Dialect.Dialect dialect)
		{
			string testDialectTypeName = "NHibernate.Test.TestDialects."
			                             + dialect.GetType().Name.Replace("Dialect", "TestDialect");
			System.Type testDialectType = System.Type.GetType(testDialectTypeName);
			if (testDialectType != null)
			{
				return (TestDialect)Activator.CreateInstance(testDialectType, dialect);
			}
			return new TestDialect(dialect);
		}

		/// <summary>
		/// Whether or not the dialect supportts the given Sql Type.
		/// </summary>
		/// <param name="sqlType">The Sql Type.</param>
		/// <returns>
		/// true if supported, false if not.
		/// </returns>
		public bool SupportsSqlType(SqlType sqlType)
		{
			try
			{
				this.dialect.GetTypeName(sqlType);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}