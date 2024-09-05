using IniHelper.Attributes;
using System.Reflection; 

namespace IniHelper
{

    public class IniSettings
    {
        private readonly string _iniFilePath;
        public IniSettings(string iniFilePath)
        {
            _iniFilePath = iniFilePath;

            // Check if the directory exists, if not, create it
            string directory = Path.GetDirectoryName(iniFilePath);
            if(!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Check if the file exists, if not, create it
            if(!File.Exists(iniFilePath))
            {
                // Creating an empty INI file
                File.Create(iniFilePath).Dispose(); // Dispose to release the file handle
            }
        }

        // Method to read the values from INI and map them to the class
        public T Read<T>() where T : new()
        {
            var obj = new T();
            Type type = typeof(T);
            IniClassAttribute classAttr = type.GetCustomAttribute<IniClassAttribute>();

            if(classAttr == null)
            {
                throw new InvalidOperationException("The class is not marked with IniClassAttribute.");
            }

            Dictionary<string, string> iniData = ParseIniFile(_iniFilePath);

            // Handle fields
            foreach(FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                IniAttribute fieldAttr = field.GetCustomAttribute<IniAttribute>();
                if(fieldAttr == null) continue;

                string key = fieldAttr.Name;
                string section = classAttr.Section;

                if(iniData.ContainsKey($"{section}.{key}"))
                {
                    string value = iniData[$"{section}.{key}"];

                    // Check if the field type is an enum
                    if(field.FieldType.IsEnum)
                    {
                        // Parse the string value into the corresponding enum
                        object enumValue = Enum.Parse(field.FieldType, value);
                        field.SetValue(obj, enumValue);
                    }
                    else
                    {
                        field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
                    }
                }
            }

            // Handle properties
            foreach(PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                IniAttribute propAttr = prop.GetCustomAttribute<IniAttribute>();
                if(propAttr == null || !prop.CanWrite) continue;

                string key = propAttr.Name;
                string section = classAttr.Section;

                if(iniData.ContainsKey($"{section}.{key}"))
                {
                    string value = iniData[$"{section}.{key}"];

                    // Check if the property type is an enum
                    if(prop.PropertyType.IsEnum)
                    {
                        // Parse the string value into the corresponding enum
                        object enumValue = Enum.Parse(prop.PropertyType, value);
                        prop.SetValue(obj, enumValue);
                    }
                    else
                    {
                        prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                    }
                }
            }

            return obj;
        }

        // Method to write the values from the class to the INI file
        public void Write<T>(T obj)
        {
            Type type = typeof(T);
            IniClassAttribute classAttr = type.GetCustomAttribute<IniClassAttribute>();

            if(classAttr == null)
            {
                throw new InvalidOperationException("The class is not marked with IniClassAttribute.");
            }

            var iniLines = new List<string>();

            // Read existing file content to avoid overwriting
            if(File.Exists(_iniFilePath))
            {
                iniLines.AddRange(File.ReadAllLines(_iniFilePath));
            }

            // Add the new section for the object
            iniLines.Add($"[{classAttr.Section}]");

            // Handle fields
            foreach(FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                IniAttribute fieldAttr = field.GetCustomAttribute<IniAttribute>();
                if(fieldAttr == null) continue;

                string key = fieldAttr.Name;
                string value = field.GetValue(obj)?.ToString() ?? string.Empty;
                iniLines.Add($"{key}={value}");
            }

            // Handle properties
            foreach(PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                IniAttribute propAttr = prop.GetCustomAttribute<IniAttribute>();
                if(propAttr == null || !prop.CanRead) continue;

                string key = propAttr.Name;
                string value = prop.GetValue(obj)?.ToString() ?? string.Empty;
                iniLines.Add($"{key}={value}");
            }

            // Write all lines back to the file
            File.WriteAllLines(_iniFilePath, iniLines);
        }


        public void WriteList<T>(List<T> objects)
        {
            Type type = typeof(T);
            IniClassAttribute classAttr = type.GetCustomAttribute<IniClassAttribute>();

            if(classAttr == null)
            {
                throw new InvalidOperationException("The class is not marked with IniClassAttribute.");
            }

            var iniLines = new List<string>();

            // Read existing file content to avoid overwriting
            if(File.Exists(_iniFilePath))
            {
                iniLines.AddRange(File.ReadAllLines(_iniFilePath));
            }

            // Add each object in the list as a new section
            for(int i = 0; i < objects.Count; i++)
            {
                iniLines.Add($"[{classAttr.Section}{i + 1}]");

                foreach(FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    IniAttribute fieldAttr = field.GetCustomAttribute<IniAttribute>();
                    if(fieldAttr == null) continue;

                    string key = fieldAttr.Name;
                    string value = field.GetValue(objects[i])?.ToString() ?? string.Empty;
                    iniLines.Add($"{key}={value}");
                }

                foreach(PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    IniAttribute propAttr = prop.GetCustomAttribute<IniAttribute>();
                    if(propAttr == null || !prop.CanRead) continue;

                    string key = propAttr.Name;
                    string value = prop.GetValue(objects[i])?.ToString() ?? string.Empty;
                    iniLines.Add($"{key}={value}");
                }

                iniLines.Add(""); // Add a blank line between object sections
            }

            // Write all lines back to the file
            File.WriteAllLines(_iniFilePath, iniLines);
        }

        public List<T> ReadList<T>() where T : new()
        {
            var objects = new List<T>();
            Type type = typeof(T);
            IniClassAttribute classAttr = type.GetCustomAttribute<IniClassAttribute>();

            if(classAttr == null)
            {
                throw new InvalidOperationException("The class is not marked with IniClassAttribute.");
            }

            Dictionary<string, string> iniData = ParseIniFile(_iniFilePath);
            int i = 1;

            while(iniData.Keys.Any(k => k.StartsWith($"{classAttr.Section}{i}.")))
            {
                var obj = new T();
                string section = $"{classAttr.Section}{i}";

                // Handle fields
                foreach(FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    IniAttribute fieldAttr = field.GetCustomAttribute<IniAttribute>();
                    if(fieldAttr == null) continue;

                    string key = fieldAttr.Name;

                    if(iniData.ContainsKey($"{section}.{key}"))
                    {
                        string value = iniData[$"{section}.{key}"];

                        // Check if field is an enum
                        if(field.FieldType.IsEnum)
                        {
                            object enumValue = Enum.Parse(field.FieldType, value);
                            field.SetValue(obj, enumValue);
                        }
                        else
                        {
                            field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
                        }
                    }
                }

                // Handle properties
                foreach(PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    IniAttribute propAttr = prop.GetCustomAttribute<IniAttribute>();
                    if(propAttr == null || !prop.CanWrite) continue;

                    string key = propAttr.Name;

                    if(iniData.ContainsKey($"{section}.{key}"))
                    {
                        string value = iniData[$"{section}.{key}"];

                        // Check if property is an enum
                        if(prop.PropertyType.IsEnum)
                        {
                            object enumValue = Enum.Parse(prop.PropertyType, value);
                            prop.SetValue(obj, enumValue);
                        }
                        else
                        {
                            prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                        }
                    }
                }

                objects.Add(obj);
                i++;
            }

            return objects;
        }

        // Helper method to parse the INI file into a dictionary
        private Dictionary<string, string> ParseIniFile(string filePath)
        {
            var iniData = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(filePath);
            string currentSection = "";

            foreach(string line in lines)
            {
                if(string.IsNullOrWhiteSpace(line) || line.StartsWith(";")) continue; // Skip empty lines and comments

                if(line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Trim('[', ']');
                }
                else
                {
                    string[] keyValue = line.Split('=');
                    if(keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();
                        iniData[$"{currentSection}.{key}"] = value;
                    }
                }
            }

            return iniData;
        }
    }
}
