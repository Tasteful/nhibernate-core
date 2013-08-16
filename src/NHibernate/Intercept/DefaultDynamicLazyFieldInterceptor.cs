using System;
using System.Threading.Tasks;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Util;

namespace NHibernate.Intercept
{
	[Serializable]
	public class DefaultDynamicLazyFieldInterceptor : IFieldInterceptorAccessor, Proxy.DynamicProxy.IInterceptor
	{
		public IFieldInterceptor FieldInterceptor { get; set; }

		public async Task<object> Intercept(InvocationInfo info)
		{
			var methodName = info.TargetMethod.Name;
			if (FieldInterceptor != null)
			{
				if (ReflectHelper.IsPropertyGet(info.TargetMethod))
				{
					if("get_FieldInterceptor".Equals(methodName))
					{
						return FieldInterceptor;
					}

					object propValue = info.InvokeMethodOnTarget();

					var result = await FieldInterceptor.Intercept(info.Target, ReflectHelper.GetPropertyName(info.TargetMethod), propValue);

					if (result != AbstractFieldInterceptor.InvokeImplementation)
					{
						return result;
					}
				}
				else if (ReflectHelper.IsPropertySet(info.TargetMethod))
				{
					if ("set_FieldInterceptor".Equals(methodName))
					{
						FieldInterceptor = (IFieldInterceptor)info.Arguments[0];
						return null;
					}
					FieldInterceptor.MarkDirty();
					await FieldInterceptor.Intercept(info.Target, ReflectHelper.GetPropertyName(info.TargetMethod), info.Arguments[0]);
				}
			}
			else
			{
				if ("set_FieldInterceptor".Equals(methodName))
				{
					FieldInterceptor = (IFieldInterceptor)info.Arguments[0];
					return null;
				}
			}

			return info.InvokeMethodOnTarget();
		}
	}
}