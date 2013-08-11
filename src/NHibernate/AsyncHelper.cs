using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NHibernate
{
	internal static class AsyncHelper
	{
		private static readonly TaskFactory MyTaskFactory;

		static AsyncHelper()
		{
			MyTaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.None, TaskContinuationOptions.None, TaskScheduler.Default);
		}

		public static TResult RunSync<TResult>(Func<Task<TResult>> func)
		{
			TaskAwaiter<TResult> awaiter = MyTaskFactory.StartNew(func).Unwrap().GetAwaiter();
			return awaiter.GetResult();
		}

		public static void RunSync(Func<Task> func)
		{
			TaskAwaiter awaiter = MyTaskFactory.StartNew(func).Unwrap().GetAwaiter();
			awaiter.GetResult();
		}
	}
}
