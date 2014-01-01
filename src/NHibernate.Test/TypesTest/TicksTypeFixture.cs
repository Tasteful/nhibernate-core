using System;
using System.Threading.Tasks;
using NHibernate.Type;
using NUnit.Framework;

namespace NHibernate.Test.TypesTest
{
	/// <summary>
	/// Summary description for TicksTypeFixture.
	/// </summary>
	[TestFixture]
	public class TicksTypeFixture
	{
		[Test]
		public async Task Next()
		{
			TicksType type = (TicksType) NHibernateUtil.Ticks;
			object current = new DateTime(2004, 1, 1, 1, 1, 1, 1);
			object next = await type.Next(current, null);

			Assert.IsTrue(next is DateTime, "Next should be DateTime");
			Assert.IsTrue((DateTime) next > (DateTime) current,
			              "next should be greater than current (could be equal depending on how quickly this occurs)");
		}

		[Test]
		public async Task Seed()
		{
			TicksType type = (TicksType) NHibernateUtil.Ticks;
			Assert.IsTrue(await type.Seed(null) is DateTime, "seed should be DateTime");
		}

		[Test]
		public async Task Comparer()
		{
			var type = (IVersionType)NHibernateUtil.Ticks;
			object v1 = await type.Seed(null);
			var v2 = v1;
			Assert.DoesNotThrow(() => type.Comparator.Compare(v1, v2));
		}
	}
}