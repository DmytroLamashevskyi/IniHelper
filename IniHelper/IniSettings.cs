using IniHelper.Attributes;
using System.Reflection;

namespace IniHelper
{
    public class IniSettings
    {
        private readonly string _iniFilePath;
        private Dictionary<string, string> _iniCache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Semaphore for synchronization

        private IniSettings(string iniFilePath)
        {
            _iniFilePath = iniFilePath ?? throw new ArgumentNullException(nameof(iniFilePath));

            string directory = Path.GetDirectoryName(iniFilePath);
            if(!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if(!File.Exists(iniFilePath))
            {
                try
                {
                    using(File.Create(iniFilePath)) { }
                }
                catch(Exception ex)
                {
                    throw new IOException($"Error creating INI file: {iniFilePath}", ex);
                }
            }

            _iniCache = new Dictionary<string, string>();
        }

        /// <summary>
        /// Factory method to create an instance of IniSettings and load the cache asynchronously.
        /// </summary>
        /// <param name="iniFilePath">The path to the INI file.</param>
        /// <returns>An instance of IniSettings with the cache loaded.</returns>
        /// <exception cref="IOException">Thrown when there is an issue reading the INI file.</exception>
        public static async Task<IniSettings> CreateAsync(string iniFilePath)
        {
            var settings = new IniSettings(iniFilePath);
            await settings.LoadCacheAsync();
            return settings;
        }

        /// <summary>
        /// Asynchronously reads the INI file and returns an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object to return, which must have a parameterless constructor.</typeparam>
        /// <returns>The populated object of type T.</returns>
        /// <exception cref="IOException">Thrown when the INI cache cannot be loaded or is empty.</exception>
        public async Task<T> ReadAsync<T>() where T : new()
        {
            await _semaphore.WaitAsync();
            try
            {
                if(_iniCache == null || _iniCache.Count == 0)
                {
                    await LoadCacheAsync();
                    if(_iniCache == null || _iniCache.Count == 0)
                    {
                        throw new IOException("Failed to load INI file or cache is empty.");
                    }
                }

                return PopulateObjectFromCache<T>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Asynchronously reads the INI file and returns a default object of type T if the cache or file cannot be loaded.
        /// </summary>
        /// <typeparam name="T">The type of the object to return, which must have a parameterless constructor.</typeparam>
        /// <returns>A populated object of type T, or a default instance if the cache cannot be loaded.</returns>
        public async Task<T> ReadSafeAsync<T>() where T : new()
        {
            await _semaphore.WaitAsync();
            try
            {
                if(_iniCache == null || _iniCache.Count == 0)
                {
                    try
                    {
                        await LoadCacheAsync();
                    }
                    catch(IOException)
                    {
                        return new T(); // Return a default object on failure
                    }
                }

                return _iniCache.Count == 0 ? new T() : PopulateObjectFromCache<T>();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Synchronously reads the INI file and returns an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object to return, which must have a parameterless constructor.</typeparam>
        /// <returns>The populated object of type T.</returns>
        /// <exception cref="IOException">Thrown when the INI cache cannot be loaded or is empty.</exception>
        public T Read<T>() where T : new()
        {
            EnsureFileAccess();

            if(_iniCache == null || _iniCache.Count == 0)
            {
                LoadCacheSync();
                if(_iniCache == null || _iniCache.Count == 0)
                {
                    throw new IOException("Failed to load INI file or cache is empty.");
                }
            }

            return PopulateObjectFromCache<T>();
        }

        /// <summary>
        /// Asynchronously writes an object to the INI file and creates a backup.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="obj">The object to write to the INI file.</param>
        /// <exception cref="IOException">Thrown when there is an issue writing to the INI file.</exception>
        public async Task WriteAsync<T>(T obj)
        {
            string backupPath = _iniFilePath + ".bak";

            if(File.Exists(_iniFilePath))
            {
                File.Copy(_iniFilePath, backupPath, true);
            }

            await _semaphore.WaitAsync();
            try
            {
                using(var writer = new StreamWriter(_iniFilePath))
                {
                    WriteObjectToFile(obj, writer);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Synchronously writes an object to the INI file.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="obj">The object to write to the INI file.</param>
        /// <exception cref="IOException">Thrown when there is an issue writing to the INI file.</exception>
        public void Write<T>(T obj)
        {
            _semaphore.Wait();
            try
            {
                using(var writer = new StreamWriter(_iniFilePath))
                {
                    WriteObjectToFile(obj, writer);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Restores the INI file from the backup copy.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the backup file does not exist.</exception>
        public void RestoreFromBackup()
        {
            string backupFilePath = _iniFilePath + ".bak";

            if(File.Exists(backupFilePath))
            {
                File.SetAttributes(backupFilePath, FileAttributes.Normal);
                File.Copy(backupFilePath, _iniFilePath, true);
            }
            else
            {
                throw new FileNotFoundException("Backup copy of the INI file not found.");
            }
        }

        /// <summary>
        /// Ensures the INI file is accessible and checks for read-only status.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the INI file is not found.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the INI file is read-only.</exception>
        private void EnsureFileAccess()
        {
            var fileInfo = new FileInfo(_iniFilePath);
            if(!fileInfo.Exists)
            {
                throw new FileNotFoundException($"INI file not found: {_iniFilePath}");
            }

            if((fileInfo.Attributes & FileAttributes.ReadOnly) != 0)
            {
                throw new UnauthorizedAccessException($"INI file is read-only: {_iniFilePath}");
            }
        }

        /// <summary>
        /// Synchronously loads the INI data into the cache.
        /// </summary>
        private void LoadCacheSync()
        {
            if(new FileInfo(_iniFilePath).Length == 0)
            {
                _iniCache = new Dictionary<string, string>();
                return;
            }

            _iniCache = ParseIniFile(_iniFilePath);
        }

        /// <summary>
        /// Asynchronously loads the INI data into the cache.
        /// </summary>
        private async Task LoadCacheAsync()
        {
            if(new FileInfo(_iniFilePath).Length == 0)
            {
                _iniCache = new Dictionary<string, string>();
                return;
            }

            _iniCache = await ParseIniFileAsync(_iniFilePath);
        }

        /// <summary>
        /// Asynchronously parses the INI file and returns the data as a dictionary.
        /// </summary>
        /// <param name="filePath">The path of the INI file to parse.</param>
        /// <returns>A dictionary containing the parsed INI file data.</returns>
        /// <exception cref="IOException">Thrown if there is an issue parsing the file.</exception>
        private async Task<Dictionary<string, string>> ParseIniFileAsync(string filePath)
        {
            var iniData = new Dictionary<string, string>();
            string currentSection = string.Empty;

            using(var reader = new StreamReader(filePath))
            {
                while(!reader.EndOfStream)
                {
                    string line = await reader.ReadLineAsync();

                    if(string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;

                    if(line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Trim('[', ']');
                    }
                    else if(line.Contains("="))
                    {
                        string[] keyValue = line.Split('=');
                        if(keyValue.Length == 2)
                        {
                            string key = keyValue[0].Trim();
                            string value = keyValue[1].Trim();
                            iniData[$"{currentSection}.{key}"] = value;
                        }
                        else
                        {
                            throw new IOException($"Malformed line: {line}");
                        }
                    }
                    else
                    {
                        throw new IOException($"Malformed line: {line}");
                    }
                }
            }

            return iniData;
        }

        /// <summary>
        /// Parses the INI file synchronously and returns the data as a dictionary.
        /// </summary>
        /// <param name="filePath">The path of the INI file to parse.</param>
        /// <returns>A dictionary containing the parsed INI file data.</returns>
        /// <exception cref="IOException">Thrown if there is an issue parsing the file.</exception>
        private Dictionary<string, string> ParseIniFile(string filePath)
        {
            var iniData = new Dictionary<string, string>();
            string currentSection = string.Empty;

            using(var reader = new StreamReader(filePath))
            {
                while(!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    if(string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue;

                    if(line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Trim('[', ']');
                    }
                    else if(line.Contains("="))
                    {
                        string[] keyValue = line.Split('=');
                        if(keyValue.Length == 2)
                        {
                            string key = keyValue[0].Trim();
                            string value = keyValue[1].Trim();
                            iniData[$"{currentSection}.{key}"] = value;
                        }
                        else
                        {
                            throw new IOException($"Malformed line: {line}");
                        }
                    }
                    else
                    {
                        throw new IOException($"Malformed line: {line}");
                    }
                }
            }

            return iniData;
        }

        /// <summary>
        /// Populates an object of type T from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the object to populate.</typeparam>
        /// <returns>The populated object of type T.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the class is not marked with IniClassAttribute.</exception>
        private T PopulateObjectFromCache<T>() where T : new()
        {
            var obj = new T();
            Type type = typeof(T);

            IniClassAttribute classAttr = type.GetCustomAttribute<IniClassAttribute>();
            if(classAttr == null)
            {
                throw new InvalidOperationException($"Class {type.Name} is not marked with IniClassAttribute.");
            }

            foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldAttr = field.GetCustomAttribute<IniAttribute>();
                if(fieldAttr == null) continue;

                string sectionKey = $"{classAttr.Section}.{fieldAttr.Name}";
                if(_iniCache.TryGetValue(sectionKey, out string value))
                {
                    if(field.FieldType.IsEnum)
                    {
                        field.SetValue(obj, Enum.Parse(field.FieldType, value));
                    }
                    else
                    {
                        field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
                    }
                }
            }

            foreach(var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propAttr = prop.GetCustomAttribute<IniAttribute>();
                if(propAttr == null || !prop.CanWrite) continue;

                string sectionKey = $"{classAttr.Section}.{propAttr.Name}";
                if(_iniCache.TryGetValue(sectionKey, out string value))
                {
                    if(prop.PropertyType.IsEnum)
                    {
                        prop.SetValue(obj, Enum.Parse(prop.PropertyType, value));
                    }
                    else
                    {
                        prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// Writes an object of type T to the INI file.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="obj">The object to write to the INI file.</param>
        /// <param name="writer">The StreamWriter used to write to the file.</param>
        /// <exception cref="InvalidOperationException">Thrown if the class is not marked with IniClassAttribute.</exception>
        private void WriteObjectToFile<T>(T obj, StreamWriter writer)
        {
            Type type = typeof(T);

            IniClassAttribute classAttr = type.GetCustomAttribute<IniClassAttribute>();
            if(classAttr == null)
            {
                throw new InvalidOperationException($"Class {type.Name} is not marked with IniClassAttribute.");
            }

            writer.WriteLine($"[{classAttr.Section}]");

            foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldAttr = field.GetCustomAttribute<IniAttribute>();
                if(fieldAttr == null) continue;

                string key = fieldAttr.Name;
                string value = field.GetValue(obj)?.ToString() ?? string.Empty;
                writer.WriteLine($"{key}={value}");
            }

            foreach(var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propAttr = prop.GetCustomAttribute<IniAttribute>();
                if(propAttr == null || !prop.CanRead) continue;

                string key = propAttr.Name;
                string value = prop.GetValue(obj)?.ToString() ?? string.Empty;
                writer.WriteLine($"{key}={value}");
            }
        }
    }
}
