using System;
using System.Linq;

namespace Sandbox
{
	/// <summary>
	/// Фабрика объектов для лока по хэшам
	/// </summary>
	public class SimpleLockFactory
	{
		private object[] _lockers;

		public SimpleLockFactory(int size)
		{
			_lockers = new object[size];
		}

		/// <summary>
		/// Изменение числа локеров.
		/// </summary>
		public void Resize(int size)
		{
			if(size <= 0 || size > 10000)
				throw new ArgumentOutOfRangeException("size");
			if (size == _lockers.Length)
				return;
			lock (this)
			{
				for (int i = 0; i < _lockers.Length; i++)
				{
					if(_lockers[i] == null)
						continue;
					var item = _lockers[i];
					lock (item)
						_lockers[i] = null;
				}
				_lockers = new object[size];
			}
		}

		/// <summary>
		/// Получить локер по набору идентификаторов.
		/// Полученный объект нужно использовать в конструкции lock(object){...} сразу же при получении.
		/// </summary>
		public object GetLocker(params object[] pars)
		{
			var tmp = pars.Aggregate(0, (current, par) => current*13 + par.GetHashCode());

			var ret = _lockers[tmp%_lockers.Length];
			if (ret != null)
				return ret;
			lock (this)
			{
				var id = tmp%_lockers.Length;
				return _lockers[id] ?? (_lockers[id] = new object());
			}
		}
	}
}