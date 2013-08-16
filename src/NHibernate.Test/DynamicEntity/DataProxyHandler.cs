using System.Collections;
using System.Threading.Tasks;
using NHibernate.Proxy.DynamicProxy;

namespace NHibernate.Test.DynamicEntity
{
	public sealed class DataProxyHandler : Proxy.DynamicProxy.IInterceptor
	{
		private readonly Hashtable data = new Hashtable();
		private readonly string entityName;

		public DataProxyHandler(string entityName, object id)
		{
			this.entityName = entityName;
			data["Id"] = id;
		}

		public string EntityName
		{
			get { return entityName; }
		}

		public Hashtable Data
		{
			get { return data; }
		}

		#region IInterceptor Members

		public Task<object> Intercept(InvocationInfo info)
		{
			string methodName = info.TargetMethod.Name;
			if ("get_DataHandler".Equals(methodName))
			{
				return Task.FromResult<object>(this);
			}
			else if (methodName.StartsWith("set_"))
			{
				string propertyName = methodName.Substring(4);
				data[propertyName] = info.Arguments[0];
			}
			else if (methodName.StartsWith("get_"))
			{
				string propertyName = methodName.Substring(4);
				return Task.FromResult<object>(data[propertyName]);
			}
			else if ("ToString".Equals(methodName))
			{
				return Task.FromResult<object>(entityName + "#" + data["Id"]);
			}
			else if ("GetHashCode".Equals(methodName))
			{
				return Task.FromResult<object>(GetHashCode());
			}
			return null;
		}

		#endregion
	}
}