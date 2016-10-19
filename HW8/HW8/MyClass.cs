using System;

namespace CS422
{
	public abstract class Dir422
	{
		public abstract string Name { get; }

		public abstract IList<Dir422> GetDirs();

		public abstract IList<File422> getFiles();

		public abstract Dir422 Parent { get;}

		public abstract bool ContainsFiles(string filename, bool recursive);

		public abstract bool ContainsDir(string dirName, bool recursive);

		public abstract File422 GetFile(string fileName);

		public abstract Dir422 GetDir(string dirName);

		public abstract File422 CreateFile(string fileName);

		public abstract Dir422 CreateName(string dirName);



	}

	public abstract class File422
	{
		public abstract string Name { get; }

		public abstract Dir422 Parent { get;}

		public abstract Stream OpenReadOnly();

		public abstract Stream OpenReadWrite();

	}

	public abstract class FileSys422{
		public abstract Dire422 GetRoot();

		public virtual bool Contains( File422 file){
			return Contains(file.Parent);
		}

		public virtual bool Contains(Dir422 dir){
			while (dir.Parent != null) {
				dir = dir.Parent;
			}
			return Object.ReferenceEquals(dir, GetRoot());
		}

	}

	public class StdFSDir : Dir422{
		private string m_path;

		public StdFSDir(string path){
			if (!Directory.Exists(path)) {
				throw new arubmentException ();
			}
			m_path = path;
		}

		public override IList<File422> GetFiles()
		{
			list<File422> files = new List<File422>();
			foreach (string file in Directory.GetFile(m_path))
			{
				files.Add(new StdFSFile(file));
			}
			return files;
	}

	public class StdFSFile : File422 {
		private string m_path;

		public override Stream OpenReadOnly()
			{
				return new fileStream (m_path, fileMode.Open, fileAccess.Read);
			}
			public override Stream OpenReadWrite(){
				throw new NOtImplementedEcteption ();
			}
	}

	public class StandardFileSystem : FileSys422{
		private Dir422 m_root;
		// just needs constructor for instatiating root
		public override Dir422 GetRoot()
		{
			return new StdFSDir (m_root);
		}
	}
}

