using System;
using System.Threading.Tasks;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Test.DynamicProxyTests
{
	public class PassThroughInterceptor : NHibernate.Proxy.DynamicProxy.IInterceptor
	{
		private readonly object targetInstance;

		public PassThroughInterceptor(object targetInstance)
		{
			this.targetInstance = targetInstance;
		}

		public Task<object> Intercept(InvocationInfo info)
		{
			return Task.FromResult(info.TargetMethod.Invoke(targetInstance, info.Arguments));
		}
	}
}