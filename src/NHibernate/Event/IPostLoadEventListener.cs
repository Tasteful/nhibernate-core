using System.Threading.Tasks;

namespace NHibernate.Event
{
	/// <summary>
	/// Occurs after an an entity instance is fully loaded.
	/// </summary>
	public interface IPostLoadEventListener
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="event"></param>
		Task OnPostLoad(PostLoadEvent @event);
	}
}