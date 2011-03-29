﻿using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Raven.Json.Linq;

namespace Raven.Json.Utilities
{
    public class CopyOnWriteJDictionary<TKey> : IDictionary<TKey, RavenJToken>, ICloneable
    {
        private static readonly RavenJToken DeletedMarker = new RavenJValue(null, JTokenType.Null);

        private IDictionary<TKey, RavenJToken> _localChanges;
        protected IDictionary<TKey, RavenJToken> LocalChanges { get { return _localChanges ?? (_localChanges = new Dictionary<TKey, RavenJToken>()); } }
        private CopyOnWriteJDictionary<TKey> _inherittedValues;

        public CopyOnWriteJDictionary()
        {
        }

        private CopyOnWriteJDictionary(IDictionary<TKey, RavenJToken> props)
        {
            _localChanges = props;
        }

        private CopyOnWriteJDictionary(CopyOnWriteJDictionary<TKey> previous)
        {
            _inherittedValues = previous;
        }

        #region Dictionary<TKey,TValue> Members

        public void Add(TKey key, RavenJToken value)
        {
            LocalChanges.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return (_inherittedValues != null && _inherittedValues.ContainsKey(key) && _inherittedValues[key] != DeletedMarker) ||
                (_localChanges != null && _localChanges.ContainsKey(key) && _localChanges[key] != DeletedMarker);
        }

        public ICollection<TKey> Keys
        {
            get
            {
            	ICollection<TKey> ret = new HashSet<TKey>();
				if (_inherittedValues != null)
				{
					foreach (var key in _inherittedValues.Keys)
					{
						if (_localChanges.ContainsKey(key))
							continue;
						ret.Add(key);
					}
				}

				if (_localChanges != null)
				{
					foreach (var key in _localChanges.Keys)
					{
						if (_localChanges[key] == DeletedMarker)
							continue;
						ret.Add(key);
					}
				}
            	return ret;
            }
        }

        public bool Remove(TKey key)
        {
            LocalChanges[key] = DeletedMarker;
            return true;
        }

        public bool TryGetValue(TKey key, out RavenJToken value)
        {
            try
            {
                value = this[key];
                return true;
            }
            catch (KeyNotFoundException)
            {
                value = null;
                return false;
            }
        }

        public ICollection<RavenJToken> Values
        {
            get
            {
            	ICollection<RavenJToken> ret = new HashSet<RavenJToken>();
            	foreach (var key in Keys)
            	{
					ret.Add(this[key]);
            	}
            	return ret;
            }
        }

        public RavenJToken this[TKey key]
        {
            get
            {
                RavenJToken val;
				if (_localChanges != null && _localChanges.TryGetValue(key, out val))
				{
					if (val == DeletedMarker)
						throw new KeyNotFoundException(key.ToString());
					return val;
				}

            	if (_inherittedValues != null && _inherittedValues.TryGetValue(key, out val))
                {
                    if (val == DeletedMarker)
						throw new KeyNotFoundException(key.ToString());

                    // Will also perform a copy-on-write clone on object supporting this
                    var safeVal = val.CloneToken();
                    LocalChanges[key] = safeVal;
                    return safeVal;
                }
            	throw new KeyNotFoundException(key.ToString());
            }
            set { LocalChanges[key] = value; }
        }

        #endregion

		public class CopyOnWriteDictEnumerator : IEnumerator<KeyValuePair<TKey, RavenJToken>>
		{
			private IEnumerator<KeyValuePair<TKey, RavenJToken>> _inheritted, _local, _current;

			public CopyOnWriteDictEnumerator(IEnumerator<KeyValuePair<TKey, RavenJToken>> inheritted, IEnumerator<KeyValuePair<TKey, RavenJToken>> local)
			{
				_inheritted = inheritted;
				_local = local;
				_current = _inheritted ?? _local;
			}

			public void Dispose()
			{
				if (_inheritted != null) _inheritted.Dispose();
				if (_local != null) _local.Dispose();
			}

			public bool MoveNext()
			{
				if (_current == null)
					return false;

				if (!_current.MoveNext())
				{
					if (_current == _inheritted && _local != null)
					{
						_current = _local;
						return _current.MoveNext();
					}
					return false;
				}
				return true;
			}

			public void Reset()
			{
				if (_inheritted != null) _inheritted.Reset();
				if (_local != null) _local.Reset();
				_current = _inheritted ?? _local;
			}

			public KeyValuePair<TKey, RavenJToken> Current
			{
				get
				{
					if (_current == null)
						throw new InvalidOperationException();
					return _current.Current;
				}
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}
		}

		public IEnumerator<KeyValuePair<TKey, RavenJToken>> GetEnumerator()
		{
			return new CopyOnWriteDictEnumerator(
				_inherittedValues != null ? _inherittedValues.GetEnumerator() : null,
				_localChanges != null ? _localChanges.GetEnumerator() : null
				);
		}

    	IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, RavenJToken> item)
        {
            Add(item.Key, item.Value);
        }

		#region Other not important operations

		public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<TKey, RavenJToken> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<TKey, RavenJToken>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, RavenJToken> item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }
        #endregion

        #region ICloneable Members

        public object Clone()
        {
            if (_inherittedValues == null)
            {
                _inherittedValues = new CopyOnWriteJDictionary<TKey>(_localChanges);
                _localChanges = null;
                return new CopyOnWriteJDictionary<TKey>(_inherittedValues);
            }
            if (_localChanges == null)
            {
                return new CopyOnWriteJDictionary<TKey>(_inherittedValues);
            }
            _inherittedValues = new CopyOnWriteJDictionary<TKey>(
                new CopyOnWriteJDictionary<TKey>(_inherittedValues) { _localChanges = _localChanges });
            _localChanges = null;
            return new CopyOnWriteJDictionary<TKey>(_inherittedValues);
        }

        #endregion
    }
}
