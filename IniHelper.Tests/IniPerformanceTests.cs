using IniHelper.Attributes;
using System.Diagnostics;
using Xunit;

namespace IniHelper.Tests
{
    public class IniPerformanceTests
    {
        private readonly string _smallIniFilePath;
        private readonly string _mediumIniFilePath;
        private readonly string _largeIniFilePath;

        public IniPerformanceTests()
        {
            // Paths to INI files of different sizes
            _smallIniFilePath = Path.Combine(Path.GetTempPath(), "small_test.ini");
            _mediumIniFilePath = Path.Combine(Path.GetTempPath(), "medium_test.ini");
            _largeIniFilePath = Path.Combine(Path.GetTempPath(), "large_test.ini");

            // Generate INI files of different sizes
            GenerateIniFile(_smallIniFilePath, 100); // 100 entries
            GenerateIniFile(_mediumIniFilePath, 1000); // 1000 entries
            GenerateIniFile(_largeIniFilePath, 10000); // 10,000 entries
        }

        private void GenerateIniFile(string filePath, int numberOfEntries)
        {
            using(var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("[TestSection]");
                for(int i = 1; i <= numberOfEntries; i++)
                {
                    writer.WriteLine($"Key{i}=Value{i}");
                }
            }
        }

        [Fact]
        public async Task TestParsingDuration_SmallFileAsync()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_smallIniFilePath);

            // Act
            var stopwatch = Stopwatch.StartNew();
            settings.Read<EmptyClass>(); // Assume EmptyClass is a class with proper Ini attributes
            stopwatch.Stop();

            // Assert
            var duration = stopwatch.ElapsedMilliseconds;
            Assert.True(duration < 500, $"Small INI file parsing took too long: {duration}ms");
        }

        [Fact]
        public async Task TestParsingDuration_MediumFile()
        {
            // Arrange
            var settings = await  IniSettings.CreateAsync(_mediumIniFilePath);

            // Act
            var stopwatch = Stopwatch.StartNew();
            settings.Read<EmptyClass>(); // Assume EmptyClass is a class with proper Ini attributes
            stopwatch.Stop();

            // Assert
            var duration = stopwatch.ElapsedMilliseconds;
            Assert.True(duration < 1000, $"Medium INI file parsing took too long: {duration}ms");
        }

        [Fact]
        public async Task TestParsingDuration_LargeFile()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_largeIniFilePath);

            // Act
            var stopwatch = Stopwatch.StartNew();
            settings.Read<EmptyClass>(); // Assume EmptyClass is a class with proper Ini attributes
            stopwatch.Stop();

            // Assert
            var duration = stopwatch.ElapsedMilliseconds;
            Assert.True(duration < 2000, $"Large INI file parsing took too long: {duration}ms");
        }

        // Cleanup temporary files after tests
        public void Dispose()
        {
            if(File.Exists(_smallIniFilePath))
                File.Delete(_smallIniFilePath);

            if(File.Exists(_mediumIniFilePath))
                File.Delete(_mediumIniFilePath);

            if(File.Exists(_largeIniFilePath))
                File.Delete(_largeIniFilePath);
        }
    }

    // A dummy class to represent an object for INI deserialization
    [IniClass("TestSection")]
    public class EmptyClass
    {
        [Ini("TestProp")]
        public string Key1 { get; set; } = Guid.NewGuid().ToString();
    }
}
