using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NHibernate
{
	/// <summary>
	/// Provides synchronous extension methods for tasks.
	/// </summary>
	internal static class TaskExtensions
	{
		/// <summary>
		/// Waits for the task to complete, unwrapping any exceptions.
		/// </summary>
		/// <param name="task">The task. May not be <c>null</c>.</param>
		public static void WaitAndUnwrapException(this Task task)
		{
			try
			{
				task.Wait();
			}
			catch (AggregateException ex)
			{
				throw ExceptionHelpers.PrepareForRethrow(ex.InnerException);
			}
		}

		/// <summary>
		/// Waits for the task to complete, unwrapping any exceptions.
		/// </summary>
		/// <param name="task">The task. May not be <c>null</c>.</param>
		/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
		/// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled before the <paramref name="task"/> completed, or the <paramref name="task"/> raised an <see cref="OperationCanceledException"/>.</exception>
		public static void WaitAndUnwrapException(this Task task, CancellationToken cancellationToken)
		{
			try
			{
				task.Wait(cancellationToken);
			}
			catch (AggregateException ex)
			{
				throw ExceptionHelpers.PrepareForRethrow(ex.InnerException);
			}
		}

		/// <summary>
		/// Waits for the task to complete, unwrapping any exceptions.
		/// </summary>
		/// <typeparam name="TResult">The type of the result of the task.</typeparam>
		/// <param name="task">The task. May not be <c>null</c>.</param>
		/// <returns>The result of the task.</returns>
		public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task)
		{
			try
			{
				return task.Result;
			}
			catch (AggregateException ex)
			{
				throw ExceptionHelpers.PrepareForRethrow(ex.InnerException);
			}
		}

		/// <summary>
		/// Waits for the task to complete, unwrapping any exceptions.
		/// </summary>
		/// <typeparam name="TResult">The type of the result of the task.</typeparam>
		/// <param name="task">The task. May not be <c>null</c>.</param>
		/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
		/// <returns>The result of the task.</returns>
		/// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled before the <paramref name="task"/> completed, or the <paramref name="task"/> raised an <see cref="OperationCanceledException"/>.</exception>
		public static TResult WaitAndUnwrapException<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
		{
			try
			{
				task.Wait(cancellationToken);
				return task.Result;
			}
			catch (AggregateException ex)
			{
				throw ExceptionHelpers.PrepareForRethrow(ex.InnerException);
			}
		}

		/// <summary>
		/// Waits for the task to complete, but does not raise task exceptions. The task exception (if any) is unobserved.
		/// </summary>
		/// <param name="task">The task. May not be <c>null</c>.</param>
		public static void WaitWithoutException(this Task task)
		{
			// Check to see if it's completed first, so we don't cause unnecessary allocation of a WaitHandle.
			if (task.IsCompleted)
			{
				return;
			}

			var asyncResult = (IAsyncResult)task;
			asyncResult.AsyncWaitHandle.WaitOne();
		}

		/// <summary>
		/// Waits for the task to complete, but does not raise task exceptions. The task exception (if any) is unobserved.
		/// </summary>
		/// <param name="task">The task. May not be <c>null</c>.</param>
		/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
		/// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was cancelled before the <paramref name="task"/> completed.</exception>
		public static void WaitWithoutException(this Task task, CancellationToken cancellationToken)
		{
			// Check to see if it's completed first, so we don't cause unnecessary allocation of a WaitHandle.
			if (task.IsCompleted)
			{
				return;
			}

			cancellationToken.ThrowIfCancellationRequested();

			var index = WaitHandle.WaitAny(new[] { ((IAsyncResult)task).AsyncWaitHandle, cancellationToken.WaitHandle });
			if (index != 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
		}
	}
	/// <summary>
	/// Provides helper (non-extension) methods dealing with exceptions.
	/// </summary>
	public static class ExceptionHelpers
	{
		private static readonly ExceptionEnlightenment ExceptionHelper = new ExceptionEnlightenment();
		/// <summary>
		/// Attempts to prepare the exception for re-throwing by preserving the stack trace. The returned exception should be immediately thrown.
		/// </summary>
		/// <param name="exception">The exception. May not be <c>null</c>.</param>
		/// <returns>The <see cref="Exception"/> that was passed into this method.</returns>
		public static Exception PrepareForRethrow(Exception exception)
		{
			return ExceptionHelper.PrepareForRethrow(exception);
		}
	}

	/// <summary>
	/// The default exception enlightenment, which will use <c>ExceptionDispatchInfo</c> if possible, falling back on <c>Exception.PrepForRemoting</c>, with a final fallback on <see cref="Exception.Data"/>.
	/// </summary>
	public sealed class ExceptionEnlightenment
	{
		/// <summary>
		/// A delegate that will call <c>ExceptionDispatchInfo.Capture</c> followed by <c>ExceptionDispatchInfo.Throw</c>, or <c>null</c> if the <c>ExceptionDispatchInfo</c> type does not exist.
		/// </summary>
		private static readonly Action<Exception> CaptureAndThrow;

		/// <summary>
		/// A delegate that will call <c>Exception.PrepForRemoting</c>, or <c>null</c> if the method does not exist. This member is always <c>null</c> if <see cref="CaptureAndThrow"/> is non-<c>null</c>.
		/// </summary>
		private static readonly Action<Exception> PrepForRemoting;

		/// <summary>
		/// Attempts to look up a method for a type, handling vexing exceptions.
		/// </summary>
		/// <param name="type">The type on which to look up the method.</param>
		/// <param name="name">The method to look up.</param>
		/// <param name="flags">The binding flags used when searching for the method.</param>
		private static MethodInfo TryGetMethod(System.Type type, string name, BindingFlags flags)
		{
			try
			{
				return type.GetMethod(name, flags);
			}
			catch (AmbiguousMatchException)
			{
				// vexing exception
				return null;
			}
		}

		/// <summary>
		/// Examines the current runtime and initializes the static delegates appropriately.
		/// </summary>
		static ExceptionEnlightenment()
		{
			var exceptionType = typeof (Exception);
			var exceptionDispatchInfoType = System.Type.GetType("System.Runtime.ExceptionServices.ExceptionDispatchInfo");
			if (exceptionDispatchInfoType != null)
			{
				try
				{
					var parameter = Expression.Parameter(exceptionType, "exception");
					var captureCall = Expression.Call(exceptionDispatchInfoType, "Capture", null, parameter);
					var throwCall = Expression.Call(captureCall, "Throw", null);
					CaptureAndThrow = Expression.Lambda<Action<Exception>>(throwCall, parameter).Compile();
				}
				catch (InvalidOperationException)
				{
					// vexing exception (Expression.Call)
				}
				catch (ArgumentException)
				{
					// vexing exception (Expression.Lambda)
				}
			}

			if (CaptureAndThrow == null)
			{
				var prepForRemotingMethod = TryGetMethod(exceptionType, "PrepForRemoting", BindingFlags.Instance | BindingFlags.NonPublic);
				if (prepForRemotingMethod != null)
				{
					try
					{
						var parameter = Expression.Parameter(exceptionType, "exception");
						var call = Expression.Call(parameter, prepForRemotingMethod);
						PrepForRemoting = Expression.Lambda<Action<Exception>>(call, parameter).Compile();
					}
					catch (ArgumentException)
					{
						// vexing exception (Expression.Call, Expression.Lambda)
					}
				}
			}
		}

		/// <summary>
		/// Attempts to add the original stack trace to the <see cref="Exception.Data"/> collection.
		/// </summary>
		/// <param name="exception">The exception. May not be <c>null</c>.</param>
		/// <returns><c>true</c> if the stack trace was successfully saved; <c>false</c> otherwise.</returns>
		private static void TryAddStackTrace(Exception exception)
		{
			try
			{
				exception.Data.Add("Original stack trace", exception.StackTrace);
			}
			catch (ArgumentException)
			{
				// Vexing exception
			}

			catch (NotSupportedException)
			{
				// Vexing exception
			}
		}

		public Exception PrepareForRethrow(Exception exception)
		{
			if (CaptureAndThrow != null)
			{
				CaptureAndThrow(exception);
			}
			else if (PrepForRemoting != null)
			{
				PrepForRemoting(exception);
			}
			else
			{
				TryAddStackTrace(exception);
			}

			return exception;
		}
	}
}
