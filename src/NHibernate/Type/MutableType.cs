using System;
using System.Collections;
using System.Threading.Tasks;
using NHibernate.Engine;
using NHibernate.SqlTypes;

namespace NHibernate.Type
{
	/// <summary>
	/// Superclass for mutable nullable types.
	/// </summary>
	[Serializable]
	public abstract class MutableType : NullableType
	{
		/// <summary>
		/// Initialize a new instance of the MutableType class using a 
		/// <see cref="SqlType"/>. 
		/// </summary>
		/// <param name="sqlType">The underlying <see cref="SqlType"/>.</param>
		protected MutableType(SqlType sqlType) : base(sqlType)
		{
		}

		/// <summary>
		/// Gets the value indicating if this IType is mutable.
		/// </summary>
		/// <value>true - a <see cref="MutableType"/> is mutable.</value>
		/// <remarks>
		/// This has been "sealed" because any subclasses are expected to be mutable.  If
		/// the type is immutable then they should inherit from <see cref="ImmutableType"/>.
		/// </remarks>
		public override sealed bool IsMutable
		{
			get { return true; }
		}

		public override async Task<object> Replace(object original, object target, ISessionImplementor session, object owner,
									   IDictionary copiedAlready)
		{
			if (IsEqual(original, target, session.EntityMode))
				return original;
			return await DeepCopy(original, session.EntityMode, session.Factory);
		}

		public abstract Task<object> DeepCopyNotNull(object value);

		public override async Task<object> DeepCopy(object value, EntityMode entityMode, ISessionFactoryImplementor factory)
		{
			return (value == null) ? null : await DeepCopyNotNull(value);
		}

	}
}