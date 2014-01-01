using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NHibernate.Cache;
using NHibernate.Cache.Entry;
using NHibernate.Collection;
using NHibernate.Engine;
using NHibernate.Event;
using NHibernate.Impl;
using NHibernate.Persister.Collection;

namespace NHibernate.Action
{
	[Serializable]
	public sealed class CollectionUpdateAction : CollectionAction
	{
		private readonly bool emptySnapshot;

		public CollectionUpdateAction(IPersistentCollection collection, ICollectionPersister persister, object key,
		                              bool emptySnapshot, ISessionImplementor session)
			: base(persister, collection, key, session)
		{
			this.emptySnapshot = emptySnapshot;
		}

		public override async Task Execute()
		{
			object id = Key;
			ISessionImplementor session = Session;
			ICollectionPersister persister = Persister;
			IPersistentCollection collection = Collection;
			bool affectedByFilters = persister.IsAffectedByEnabledFilters(session);

			bool statsEnabled = session.Factory.Statistics.IsStatisticsEnabled;
			Stopwatch stopwatch = null;
			if (statsEnabled)
			{
				stopwatch = Stopwatch.StartNew();
			}

			PreUpdate();

			if (!collection.WasInitialized)
			{
				if (!collection.HasQueuedOperations)
				{
					throw new AssertionFailure("no queued adds");
				}
				//do nothing - we only need to notify the cache...
			}
			else if (!affectedByFilters && collection.Empty)
			{
				if (!emptySnapshot)
				{
					await persister.Remove(id, session);
				}
			}
			else if (collection.NeedsRecreate(persister))
			{
				if (affectedByFilters)
				{
					throw new HibernateException("cannot recreate collection while filter is enabled: "
					                             + MessageHelper.InfoString(persister, id, persister.Factory));
				}
				if (!emptySnapshot)
				{
					await persister.Remove(id, session);
				}
				await persister.Recreate(collection, id, session);
			}
			else
			{
				await persister.DeleteRows(collection, id, session);
				await persister.UpdateRows(collection, id, session);
				await persister.InsertRows(collection, id, session);
			}

			await Session.PersistenceContext.GetCollectionEntry(collection).AfterAction(collection);

			Evict();

			PostUpdate();

			if (statsEnabled)
			{
				stopwatch.Stop();
				Session.Factory.StatisticsImplementor.UpdateCollection(Persister.Role, stopwatch.Elapsed);
			}
		}

		private void PreUpdate()
		{
			IPreCollectionUpdateEventListener[] preListeners = Session.Listeners.PreCollectionUpdateEventListeners;
			if (preListeners.Length > 0)
			{
				PreCollectionUpdateEvent preEvent = new PreCollectionUpdateEvent(Persister, Collection, (IEventSource)Session);
				for (int i = 0; i < preListeners.Length; i++)
				{
					preListeners[i].OnPreUpdateCollection(preEvent);
				}
			}
		}

		private void PostUpdate()
		{
			IPostCollectionUpdateEventListener[] postListeners = Session.Listeners.PostCollectionUpdateEventListeners;
			if (postListeners.Length > 0)
			{
				PostCollectionUpdateEvent postEvent = new PostCollectionUpdateEvent(Persister, Collection, (IEventSource)Session);
				for (int i = 0; i < postListeners.Length; i++)
				{
					postListeners[i].OnPostUpdateCollection(postEvent);
				}
			}
		}

		public override BeforeTransactionCompletionProcessDelegate BeforeTransactionCompletionProcess
		{
			get 
			{ 
				return null; 
			}
		}

		public override AfterTransactionCompletionProcessDelegate AfterTransactionCompletionProcess
		{
			get
			{
				return new AfterTransactionCompletionProcessDelegate((success) =>
				{
					// NH Different behavior: to support unlocking collections from the cache.(r3260)
					if (Persister.HasCache)
					{
						CacheKey ck = Session.GenerateCacheKey(Key, Persister.KeyType, Persister.Role);

						if (success)
						{
							// we can't disassemble a collection if it was uninitialized 
							// or detached from the session
							if (Collection.WasInitialized && Session.PersistenceContext.ContainsCollection(Collection))
							{
								CollectionCacheEntry entry = CollectionCacheEntry.Create(Collection, Persister).WaitAndUnwrapException();
								bool put = Persister.Cache.AfterUpdate(ck, entry, null, Lock);
		
								if (put && Session.Factory.Statistics.IsStatisticsEnabled)
								{
									Session.Factory.StatisticsImplementor.SecondLevelCachePut(Persister.Cache.RegionName);
								}
							}
						}
						else
						{
							Persister.Cache.Release(ck, Lock);
						}
					}
				});
			}
		}
	}
}