using System;
using System.Linq;
using System.Threading;

namespace Sandbox
{
	/// <summary>
	/// Фабрика ReaderWriterLockSlim'ов по хэшам
	/// </summary>
	public class ReadWriteLockSlimFactory
	{
		private ReaderWriterLockSlim[] _lockers;

		public ReadWriteLockSlimFactory(int size)
		{
			_lockers = new ReaderWriterLockSlim[size];
		}

		/// <summary>
		/// Изменение числа локеров.
		/// [ToDo] Сама процедура ресайзинга временно нарушает потоковую безопасность. Но если её использовать редко, а сами локи достаточно короткие, то всё должно быть ок.
		/// </summary>
		public void Resize(int size)
		{
			if(size <= 0 || size > 10000)
				throw new ArgumentOutOfRangeException("size");
			if (size == _lockers.Length)
				return;
			ReaderWriterLockSlim[] tmp;
			lock (this)
			{
				foreach (var t in _lockers.Where(t => t != null))
					t.EnterWriteLock();
				tmp = _lockers;
				_lockers = new ReaderWriterLockSlim[size];
			}
			foreach (var t in tmp.Where(t => t != null))
			{
				t.ExitWriteLock();
				t.Dispose();
			}
		}

		/// <summary>
		/// Получить локер по набору идентификаторов.
		/// Напрямую пользоваться этим методом не рекомендуется, но в некоторых случаях придётся.
		/// </summary>
		public ReaderWriterLockSlim GetLocker(params object[] pars)
		{
			var tmp = pars.Aggregate(0, (current, par) => current*13 + par.GetHashCode());

			var ret = _lockers[tmp%_lockers.Length];
			if (ret != null)
				return ret;
			lock (this)
			{
				var id = tmp%_lockers.Length;
				ret = _lockers[id];
				if (ret != null)
					return ret;
				ret = _lockers[id] = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
				return ret;
			}
		}

		/// <summary>
		/// Открыть лок на чтение по набору идентификаторов.
		/// Лок закроется после вызова Dispose у возвращаемого объекта.
		/// </summary>
		public ReadLock GetReadLock(params object[] pars)
		{
			return GetLocker(pars).ToRead();
		}

		/// <summary>
		/// Открыть лок на запись по набору идентификаторов.
		/// Лок закроется после вызова Dispose у возвращаемого объекта.
		/// </summary>
		public WriteLock GetWriteLock(params object[] pars)
		{
			return GetLocker(pars).ToWrite();
		}
	}

	/// <summary>
	/// Методы расширения для работы с ReaderWriterLockSlim.
	/// </summary>
	public static class ReaderWriterLockSlimExt
	{
		/// <summary>
		/// Возвращает объект ReadLock, привязанный к начатому локу на чтение.
		/// </summary>
		public static ReadLock ToRead(this ReaderWriterLockSlim locker)
		{
			return new ReadLock(locker);
		}

		/// <summary>
		/// Возвращает объект ReadLock, привязанный к начатому локу на запись.
		/// </summary>
		public static WriteLock ToWrite(this ReaderWriterLockSlim locker)
		{
			return new WriteLock(locker);
		}
	}

	/// <summary>
	/// Объект, связанный с локом на чтение.
	/// </summary>
	public class ReadLock : IDisposable
	{
		private readonly ReaderWriterLockSlim _locker;

		public ReadLock(ReaderWriterLockSlim locker)
		{
			_locker = locker;
			_locker.EnterReadLock();
		}

		public void Dispose()
		{
			_locker.ExitReadLock();
		}
	}

	/// <summary>
	/// Объект, связанный с локом на запись.
	/// </summary>
	public class WriteLock : IDisposable
	{
		private readonly ReaderWriterLockSlim _locker;

		public WriteLock(ReaderWriterLockSlim locker)
		{
			_locker = locker;
			_locker.EnterWriteLock();
		}

		public void Dispose()
		{
			_locker.ExitWriteLock();
		}
	}
}