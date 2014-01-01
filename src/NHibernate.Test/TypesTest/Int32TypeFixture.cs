using System;
using System.Threading.Tasks;
using NHibernate.Type;
using NUnit.Framework;

namespace NHibernate.Test.TypesTest
{
	/// <summary>
	/// Summary description for Int32TypeFixture.
	/// </summary>
	[TestFixture]
	public class Int32TypeFixture
	{
		[Test]
		public async Task Next()
		{
			Int32Type type = (Int32Type) NHibernateUtil.Int32;
			object current = (int) 1;
			object next = await type.Next(current, null);

			Assert.IsTrue(next is Int32, "Next should be Int32");
			Assert.AreEqual((int) 2, (int) next, "current should have been incremented to 2");
		}

		[Test]
		public async Task Seed()
		{
			Int32Type type = (Int32Type) NHibernateUtil.Int32;
			Assert.IsTrue(await type.Seed(null) is Int32, "seed should be Int32");
		}
	}
}