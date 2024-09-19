using System.Reflection;
using System.Text;

namespace Glacc.Config
{
	class ConfigFile
	{
		string configFilename;
		string configFilePath;

		FileStream? configFile;
		StreamWriter? writer;

		const string defaultFileName = "config.ini";

		struct ConfigItem
		{
			public string name;
			public string value;
		}

		struct ConfigTag
		{
			public string name;
			public List<ConfigItem> items;
		}

		List<ConfigTag> tags = new List<ConfigTag>();
		ConfigTag currentTag;
		bool tagSelected = false;

		bool edited = false;

		public void SetTag(string tagName)
		{
			foreach (ConfigTag tag in tags)
			{
				if (tag.name == tagName)
				{
					currentTag = tag;

					tagSelected = true;

					return;
				}
			}

			ConfigTag newTag;
			newTag.name = tagName;
			newTag.items = new List<ConfigItem>();

			tags.Add(newTag);
			currentTag = tags[tags.Count - 1];

			edited = tagSelected = true;
		}

		#region Write
		public bool Write(string name, string value)
		{
			if (!tagSelected)
				return false;

			edited = true;

			ConfigItem newItem;
			newItem.name = name;
			newItem.value = value;

			int i = 0;
			while (i < currentTag.items.Count)
			{
				if (currentTag.items[i].name == name)
				{
					currentTag.items[i] = newItem;

					return true;
				}

				i++;
			}

			currentTag.items.Add(newItem);

			return true;
		}
		public void Write(string tag, string name, string value)
		{
			SetTag(tag);

			Write(name, value);
		}
		#endregion

		#region Read
		public string Read(string name, string defaultValue = "")
		{
			foreach (ConfigItem item in currentTag.items)
			{
				if (item.name == name)
					return item.value;
			}

			Write(name, defaultValue);

			return defaultValue;
		}

		public string Read(string tag, string name, string defaultValue = "")
		{
			SetTag(tag);

			return Read(name, defaultValue);
		}
		#endregion

		public bool Load()
		{
			if (!File.Exists(configFilePath))
				return false;

			configFile = new FileStream(configFilePath, FileMode.OpenOrCreate, FileAccess.Read);

			byte[] configFileBuffer = new byte[configFile.Length];
			configFile.Read(configFileBuffer, 0, (int)configFile.Length);
			string configFileString = Encoding.UTF8.GetString(configFileBuffer);

			ConfigTag currentTag;
			currentTag.items = new List<ConfigItem>();
			string[] lines = configFileString.Split("\n");
			foreach(string line in lines)
			{
				if (line.StartsWith('[') && line.EndsWith("]"))
				{
					ConfigTag tag;
					tag.name = line.Substring(1, line.Length - 2);
					tag.items = new List<ConfigItem>();

					tags.Add(tag);
					currentTag = tags[tags.Count - 1];
				}
				else if (line.Length > 0)
				{
					ConfigItem item;

					string[] strings = line.Split("=");
					item.name = strings[0];

					item.value = "";
					int i = 1;
					while (i < strings.Length)
					{
						item.value += strings[i];
						i++;
					}

					currentTag.items.Add(item);
				}
			}

			configFile.Close();

			return true;
		}

		public void Save()
		{
			if (!edited)
				return;

			string outputString = "";

			foreach (ConfigTag tag in tags)
			{
				outputString += $"[{tag.name}]\n";
				foreach (ConfigItem item in tag.items)
					outputString += $"{item.name}={item.value}\n";

				outputString += '\n';
			}

			configFile = new FileStream(configFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
			configFile.SetLength(0);

			writer = new StreamWriter(configFile);
			writer.Write(outputString);
			writer.Close();

			configFile.Close();

			edited = false;
		}

		public ConfigFile(string? filename, bool load = true) 
		{
			edited = false;

			configFilename = filename != null ? filename : defaultFileName;

			string assemblyPath = string.Join('\\', Assembly.GetExecutingAssembly().Location.Split("\\").SkipLast(1));
			configFilePath = Path.Combine(assemblyPath, configFilename);

			if (load)
				Load();
		}
	}
}
