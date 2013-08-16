using System.Collections.Generic;
using System.Threading.Tasks;
using NHibernate.Proxy.DynamicProxy;
using NUnit.Framework;
using SharpTestsEx;

namespace NHibernate.Test.DynamicProxyTests
{
	public class InterfaceWithEqualsGethashcodeTests
	{
		public interface IMyBaseObject
		{
			bool Equals(object that);
			int GetHashCode();
		}
		public interface IHasSomething : IMyBaseObject
		{
			string Something { get; set; }
		}
		public class InterceptedMethodsExposer : Proxy.DynamicProxy.IInterceptor
		{
			private readonly List<string> interceptedMethods = new List<string>();
			public Task<object> Intercept(InvocationInfo info)
			{
				interceptedMethods.Add(info.TargetMethod.Name);
				return Task.FromResult<object>(true);
			}

			public IEnumerable<string> InterceptedMethods
			{
				get { return interceptedMethods; }
			}
		}

		[Test]
		public void WhenProxyAnInterfaceShouldInterceptEquals()
		{
			var proxyFactory = new ProxyFactory();
			var interceptor = new InterceptedMethodsExposer();
			var proxy = proxyFactory.CreateProxy(typeof(IHasSomething), interceptor, null);
			proxy.Equals(null);
			interceptor.InterceptedMethods.Should().Contain("Equals");
		}
	}
}