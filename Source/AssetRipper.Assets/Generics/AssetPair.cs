namespace AssetRipper.Assets.Generics
{
	public sealed class AssetPair<TKey, TValue> : AccessPairBase<TKey, TValue>
		where TKey : notnull, new()
        where TValue : notnull
	{
        public AssetPair() 
		{
			Key = new TKey();
		}

        public AssetPair(ref KeyValuePair<TKey, TValue> pair) : this(pair.Key, pair.Value)
		{
		}

		public AssetPair(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}

		public override TKey Key { get; set; }
		public override TValue Value { get; set; }
	}
}
