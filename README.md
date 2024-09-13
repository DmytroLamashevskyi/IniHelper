# IniHelper

IniHelper is a .NET library designed to simplify working with INI configuration files. It provides an easy-to-use API for reading and writing INI files, with support for caching, synchronization, and error handling. This tool is designed for developers looking for a robust solution for managing configuration in INI format, with features like backup, async methods, and safe read functionality.

## Features

- **Read and Write INI Files**: Supports reading and writing data to INI files with custom class attributes.
- **Asynchronous Support**: Includes async methods for non-blocking IO operations.
- **Backup and Restore**: Automatically creates a backup of the INI file before writing and provides a restore functionality.
- **Thread-Safe**: Uses semaphores to handle concurrent read/write operations, ensuring thread safety.
- **Safe Reading**: Provides a safe read mode that returns default values when the INI file is corrupted or missing.
- **Performance Testing**: Includes built-in performance tests to evaluate parsing time for small, medium, and large INI files.

## Installation

To use IniHelper in your project, you can add it as a NuGet package:

```bash
dotnet add package IniHelper
```

# Usage

## Basic Usage

You can annotate your classes with the provided `IniClass` and `Ini` attributes to map class fields and properties to INI file sections and keys.

Example:

```csharp
[IniClass("AppSettings")]
public class AppConfig
{
    [Ini("Setting1")]
    public string Setting1 { get; set; }

    [Ini("Setting2")]
    public int Setting2 { get; set; }
}
```

## Reading from INI File

To read data from an INI file, use the `ReadAsync` method. The data will be mapped to your annotated class.

```csharp
var settings = await IniSettings.CreateAsync("path_to_file.ini");
var config = await settings.ReadAsync<AppConfig>();
```

## Writing to INI File

You can write data to an INI file by passing an instance of your annotated class to the `WriteAsync` method.


```csharp
var config = new AppConfig { Setting1 = "Value", Setting2 = 123 };
await settings.WriteAsync(config);
```

## Safe Reading

If you want to safely read an INI file and avoid exceptions in case the file is missing or corrupted, you can use the `ReadSafeAsync` method. It will return default values if the file is not available or if the cache is empty.

```csharp
var config = await settings.ReadSafeAsync<AppConfig>();
```

## Restore from Backup

You can restore the INI file from its automatically created backup:

```csharp
settings.RestoreFromBackup();
```

## Restore from Backup

IniHelper includes tests to measure the performance of reading INI files of various sizes.

To run the performance tests, use the provided test methods in the `IniPerformanceTests` class. These tests verify that the library can handle small, medium, and large INI files within a reasonable time frame.

```bash
dotnet test
```

The tests will parse INI files of varying sizes (100, 1000, and 10,000 entries) and measure the time taken to read them.


## Contributing

Contributions are welcome! Please feel free to submit a pull request or open an issue if you find any bugs or have suggestions for new features.

## License

IniHelper is licensed under the MIT License. See the LICENSE file for more details.

## Contact
For any questions or support, feel free to reach out by opening an issue on the GitHub repository.


### Key Points Included:
- **Overview** of what the tool does.
- **Features**: Highlighting the core functionality of the tool.
- **Installation**: Information on how to install the library.
- **Usage Examples**: Showing how to use the tool for reading and writing INI files, including async methods and safe reading.
- **Performance Testing**: Explanation of the built-in performance tests.
- **Contributing**: Instructions for developers who want to contribute.
- **License**: Reference to the license for the project.

This `README.md` file should provide a good foundation for explaining the functionality of `IniHelper` to new users and developers.
