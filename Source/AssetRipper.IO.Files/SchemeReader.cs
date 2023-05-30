using AssetRipper.IO.Files.BundleFiles.Archive;
using AssetRipper.IO.Files.BundleFiles.FileStream;
using AssetRipper.IO.Files.BundleFiles.RawWeb.Raw;
using AssetRipper.IO.Files.BundleFiles.RawWeb.Web;
using AssetRipper.IO.Files.CompressedFiles.Brotli;
using AssetRipper.IO.Files.CompressedFiles.GZip;
using AssetRipper.IO.Files.ResourceFiles;
using AssetRipper.IO.Files.SerializedFiles;
using AssetRipper.IO.Files.Streams.MultiFile;
using AssetRipper.IO.Files.WebFiles;

namespace AssetRipper.IO.Files
{
	public static class SchemeReader
	{
		private static readonly Stack<IScheme> schemes = new()
		{
			SerializedFileScheme.Default,
			new GZipFileScheme(),
			new BrotliFileScheme(),
			new WebFileScheme(),
			new ArchiveBundleScheme(),
			new WebBundleScheme(),
			new RawBundleScheme(),
			new FileStreamBundleScheme(),
		};

		public static FileBase LoadFile(string filePath) => LoadFile(filePath, Path.GetFileName(filePath));

		public static FileBase LoadFile(string filePath, string fileName)
		{
			var ms = MultiFileStream.OpenReadSingle(filePath);
			return ReadFile(ms.CreateAccessor(), filePath, fileName);
		}

		public static FileBase ReadFile(MemoryAreaAccessor stream, string filePath, string fileName)
		{
			long initialpos = stream.Position;
			foreach (IScheme scheme in schemes)
			{
				if (scheme.CanRead(stream))
				{
					return scheme.Read(stream, filePath, fileName);
				}
				stream.Position = initialpos;
			}

			return new ResourceFile(stream, filePath, fileName);
		}

		public static FileBase ReadFile(ResourceFile file)
		{
			return ReadFile(file.MemoryView, file.FilePath, file.Name);
		}

		public static bool IsReadableFile(string filePath)
		{
			using MemoryMappedFileWrapper file = new MemoryMappedFileWrapper(filePath);
			foreach (IScheme scheme in schemes)
			{
				if (scheme.CanRead(file.CreateAccessor()))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Required for the initialization of <see cref="schemes"/>
		/// </summary>
		private static void Add(this Stack<IScheme> stack, IScheme scheme) => stack.Push(scheme);
	}
}
