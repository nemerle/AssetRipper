using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Interfaces;
using AssetRipper.Assets.Metadata;
using AssetRipper.IO;
using AssetRipper.IO.Files.ResourceFiles;

namespace AssetRipper.GUI
{
	public sealed class DummyAssetForLooseResourceFile : UnityObjectBase, IDisposable, IHasNameString
	{
		private bool disposedValue;

		public ResourceFile AssociatedFile { get; }

		public string NameString
		{
			get => AssociatedFile.Name;
			set => throw new NotSupportedException();
		}

		public override string ClassName => nameof(DummyAssetForLooseResourceFile);

		private readonly MemoryAreaAccessor smartStream;

		public DummyAssetForLooseResourceFile(ResourceFile associatedFile) : base(MakeDummyAssetInfo())
		{
			AssociatedFile = associatedFile;
			smartStream = AssociatedFile.MemoryView.CreateSubAccessor();
		}

		public void SaveToFile(string path)
		{
			using FileStream fileStream = File.Create(path);
			smartStream.Position = 0;
			smartStream.CopyTo(fileStream);
		}

		public async Task SaveToFileAsync(string path)
		{
			FileStream fileStream = File.Create(path);
			smartStream.Position = 0;
			await smartStream.CopyToAsync(fileStream);
			await fileStream.FlushAsync();
		}

		private void Dispose(bool disposing)
		{
			if (!disposedValue)
			{

				AssociatedFile?.Dispose();
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private static AssetInfo MakeDummyAssetInfo()
		{
			return new AssetInfo(dummyBundle.Collection, 0, -1);
		}

		private static readonly DummyBundle dummyBundle = new();

		private sealed class DummyAssetCollection : AssetCollection
		{
			public DummyAssetCollection(Bundle bundle) : base(bundle)
			{
			}
		}

		private sealed class DummyBundle : Bundle
		{
			public DummyAssetCollection Collection { get; }
			public override string Name => nameof(DummyBundle);
			public DummyBundle()
			{
				Collection = new DummyAssetCollection(this);
			}
		}
	}
}
