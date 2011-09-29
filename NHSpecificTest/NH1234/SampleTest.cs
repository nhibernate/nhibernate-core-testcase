using NHibernate.Dialect;

using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH1234
{
	[TestFixture]
	public class SampleTest : BugTestCase
	{
		[Test]
		public void BytePropertyShouldBeRetrievedCorrectly()
		{
			using (ISession session = this.OpenSession())
			{
				DomainClass entity = session.Get<DomainClass>(1);

				Assert.AreEqual(3, entity.ByteData.Length);
				Assert.AreEqual(1, entity.ByteData[0]);
				Assert.AreEqual(2, entity.ByteData[1]);
				Assert.AreEqual(3, entity.ByteData[2]);
			}
		}

		protected override bool AppliesTo(Dialect.Dialect dialect)
		{
			return dialect as MsSql2005Dialect != null;
		}

		protected override void OnSetUp()
		{
			base.OnSetUp();
			using (ISession session = this.OpenSession())
			{
				DomainClass entity = new DomainClass();
				entity.Id = 1;
				entity.ByteData = new byte[] { 1, 2, 3 };
				session.Save(entity);
				session.Flush();
			}
		}

		protected override void OnTearDown()
		{
			base.OnTearDown();
			using (ISession session = this.OpenSession())
			{
				string hql = "from System.Object";
				session.Delete(hql);
				session.Flush();
			}
		}
	}
}