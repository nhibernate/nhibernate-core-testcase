using System.Collections;

namespace NHibernate.Test.NHSpecificTest
{
	/// <summary>
	/// Base class that can be used for tests in NH* subdirectories. Assumes all mappings are in a single file named
	/// <c>Mappings.hbm.xml</c>
	/// in the subdirectory.
	/// </summary>
	public abstract class BugTestCase : TestCase
	{
		/// <summary>
		/// Gets the bug number.
		/// </summary>
		public virtual string BugNumber
		{
			get
			{
				string ns = this.GetType().Namespace;
				return ns.Substring(ns.LastIndexOf('.') + 1);
			}
		}

		/// <summary>
		/// Mapping files used in the TestCase.
		/// </summary>
		protected override IList Mappings
		{
			get
			{
				return new[]
				{
					"NHSpecificTest." + this.BugNumber + ".Mappings.hbm.xml"
				};
			}
		}

		/// <summary>
		/// Assembly to load mapping files from (default is NHibernate.DomainModel).
		/// </summary>
		protected override string MappingsAssembly
		{
			get { return "NHibernate.Test"; }
		}
	}
}