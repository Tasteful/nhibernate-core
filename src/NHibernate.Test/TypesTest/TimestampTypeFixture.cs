using System;
using System.Threading.Tasks;
using NHibernate.Type;
using NUnit.Framework;

namespace NHibernate.Test.TypesTest
{
	/// <summary>
	/// Summary description for TimestampTypeFixture.
	/// </summary>
	[TestFixture]
	public class TimestampTypeFixture
	{
		[Test]
		public async Task Next()
		{
			TimestampType type = (TimestampType) NHibernateUtil.Timestamp;
			object current = DateTime.Parse("2004-01-01");
			object next = await type.Next(current, null);

			Assert.IsTrue(next is DateTime, "Next should be DateTime");
			Assert.IsTrue((DateTime) next > (DateTime) current,
			              "next should be greater than current (could be equal depending on how quickly this occurs)");
		}

		[Test]
		public async Task Seed()
		{
			TimestampType type = (TimestampType) NHibernateUtil.Timestamp;
			Assert.IsTrue(await type.Seed(null) is DateTime, "seed should be DateTime");
		}
	}
}