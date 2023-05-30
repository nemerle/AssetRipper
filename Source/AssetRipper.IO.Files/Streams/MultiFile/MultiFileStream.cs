using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AssetRipper.IO.Files.Streams.MultiFile
{
	public sealed class MultiFileStream : IDisposable
	{
        public MultiFileStream(IEnumerable<MemoryMappedFileWrapper?> streams)
		{
			if (streams == null)
			{
				throw new ArgumentNullException(nameof(streams));
			}
			foreach (MemoryMappedFileWrapper? stream in streams)
			{
				if (stream == null)
				{
					throw new ArgumentNullException();
				}
			}

			m_files = streams.ToArray();
			if (m_files.Count == 0)
			{
				throw new ArgumentException(null, nameof(streams));
			}
		}

		~MultiFileStream()
		{
			Dispose(false);
		}

		/// <summary>
		/// Determines if the path could be part of a multi file
		/// </summary>
		/// <param name="path">The path to check</param>
		/// <returns>True if the path matches the multi file regex</returns>
		public static bool IsMultiFile(string path)
		{
			return s_splitCheck.IsMatch(path);
		}

		public static bool Exists(string path)
		{
			if (IsMultiFile(path))
			{
				SplitPathWithoutExtension(path, out string directory, out string file);
				return Exists(directory, file);
			}
			if (File.Exists(path))
			{
				return true;
			}

			{
				SplitPath(path, out string directory, out string file, true);
				if (string.IsNullOrEmpty(file))
				{
					return false;
				}
				return Exists(directory, file);
			}
		}

        public static MultiFileStream OpenRead(string path)
		{
			if (IsMultiFile(path))
			{
				SplitPathWithoutExtension(path, out string directory, out string file);
				return OpenRead(directory, file);
			}
			if (File.Exists(path))
			{
                return new MultiFileStream( new[] { new MemoryMappedFileWrapper(path) } );
			}

			{
				SplitPath(path, out string directory, out string file);
				return OpenRead(directory, file);
			}
		}

		public static string GetFilePath(string path)
		{
			if (IsMultiFile(path))
			{
				int index = path.LastIndexOf('.');
				return path.Substring(0, index);
			}
			return path;
		}

		public static string GetFileName(string path)
		{
			if (IsMultiFile(path))
			{
				return Path.GetFileNameWithoutExtension(path);
			}
			return Path.GetFileName(path);
		}

		public static string[] GetFiles(string path)
		{
			if (IsMultiFile(path))
			{
				SplitPathWithoutExtension(path, out string directory, out string file);
				return GetFiles(directory, file);
			}

			if (File.Exists(path))
			{
				return new[] { path };
			}
			return Array.Empty<string>();
		}

		public static bool IsNameEquals(string fileName, string compare)
		{
			fileName = GetFileName(fileName);
			return fileName == compare;
		}

		/// <summary>
		/// Determines if a multi file exists
		/// </summary>
		/// <param name="dirPath">The directory containing the multi file</param>
		/// <param name="fileName">The name of the multi file without the split extension</param>
		/// <returns>True if a valid multi file exists in that directory with that name</returns>
		private static bool Exists(string dirPath, string fileName)
		{
			string filePath = Path.Combine(dirPath, fileName);
			string splitFilePath = filePath + ".split";

			string[] splitFiles = GetFiles(dirPath, fileName);
			if (splitFiles.Length == 0)
			{
				return false;
			}

			for (int i = 0; i < splitFiles.Length; i++)
			{
				string indexFileName = splitFilePath + i;
				if (!splitFiles.Contains(indexFileName))
				{
					return false;
				}
			}
			return true;
		}

		private static string[] GetFiles(string dirPath, string fileName)
		{
			if (!Directory.Exists(dirPath))
			{
				return Array.Empty<string>();
			}

			string filePatern = fileName + ".split*";
			return Directory.GetFiles(dirPath, filePatern);
		}

        private static Dictionary<string, MemoryMappedFileWrapper> s_wrapped = new();
        public static MemoryMappedFileWrapper OpenReadSingle(string path)
        {
			if(s_wrapped.TryGetValue(path, out MemoryMappedFileWrapper? wrapper))
            {
                return wrapper;
            }
			if (IsMultiFile(path))
            {
                throw new ArgumentException("Path is a multi file", nameof(path));
            }
			if (File.Exists(path))
            {
                var res= new MemoryMappedFileWrapper(path);
                s_wrapped[path] = res;
                return res;
            }
			throw new ArgumentException("File does not exist");
		}

        private static MultiFileStream OpenRead(string dirPath, string fileName)
		{
			string filePath = Path.Combine(dirPath, fileName);
			string splitFilePath = filePath + ".split";

			string[] splitFiles = GetFiles(dirPath, fileName);
			for (int i = 0; i < splitFiles.Length; i++)
			{
				string indexFileName = splitFilePath + i;
				if (!splitFiles.Contains(indexFileName))
				{
					throw new Exception($"Try to open splited file part '{filePath}' but file part '{indexFileName}' wasn't found");
				}
			}

			splitFiles = splitFiles.OrderBy(t => t, s_splitNameComparer).ToArray();
			MemoryMappedFileWrapper?[] streams = new MemoryMappedFileWrapper[splitFiles.Length];
			try
			{
				for (int i = 0; i < splitFiles.Length; i++)
				{
                    streams[i] = new MemoryMappedFileWrapper(splitFiles[i]);
				}

				return new MultiFileStream(streams);
			}
			catch
			{
                foreach (MemoryMappedFileWrapper? stream in streams)
				{
					if (stream == null)
					{
						break;
					}
					stream.Dispose();
				}
				throw;
			}
		}

		private static void SplitPath(string path, out string directory, out string file) => SplitPath(path, out directory, out file, false);
		private static void SplitPath(string path, out string directory, out string file, bool allowNullReturn)
		{
			directory = Path.GetDirectoryName(path) ?? throw new Exception("Could not get directory name");
			directory = string.IsNullOrEmpty(directory) ? "." : directory;
			file = Path.GetFileName(path);
			if (string.IsNullOrEmpty(file) && !allowNullReturn)
			{
				throw new Exception($"Can't determine file name for {path}");
			}
		}

		private static void SplitPathWithoutExtension(string path, out string directory, out string file)
		{
			directory = Path.GetDirectoryName(path) ?? throw new Exception("Could not get directory name");
			directory = string.IsNullOrEmpty(directory) ? "." : directory;
			file = Path.GetFileNameWithoutExtension(path);
			if (string.IsNullOrEmpty(file))
			{
				throw new Exception($"Can't determine file name for {path}");
			}
		}

        public void SetLength(long value)
		{
			throw new NotSupportedException();
		}

        public void Dispose()
        {
			Dispose(disposing: true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
        protected void Dispose(bool disposing)
        {
			foreach (var fl in m_files)
            {
                fl.Dispose();
            }
		}

		private static readonly Regex s_splitCheck = new Regex($@".+{MultifileRegPostfix}[0-9]+$", RegexOptions.Compiled);
		private static readonly SplitNameComparer s_splitNameComparer = new SplitNameComparer();

		public const string MultifileRegPostfix = @"\.split";

		/// <summary>
		/// Always has at least one element.
		/// </summary>
        private readonly IReadOnlyList<MemoryMappedFileWrapper> m_files;
	}
}
