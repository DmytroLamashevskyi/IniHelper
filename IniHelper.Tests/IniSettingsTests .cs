using IniHelper.Attributes;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace IniHelper.Tests
{
    public class IniSettingsTests : IDisposable
    {
        private readonly string _testIniFilePath;

        public IniSettingsTests()
        {
            _testIniFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ini");
        }

        public void Dispose()
        {
            // Reset the attributes of the original INI file and its backup (if they exist)
            if(File.Exists(_testIniFilePath))
            {
                File.SetAttributes(_testIniFilePath, FileAttributes.Normal);
                File.Delete(_testIniFilePath);
            }

            var backupFilePath = _testIniFilePath + ".bak";
            if(File.Exists(backupFilePath))
            {
                File.SetAttributes(backupFilePath, FileAttributes.Normal);
                File.Delete(backupFilePath);
            }
        }


        [Fact]
        public async Task ReadAsync_ShouldReloadCacheIfEmptyAndFileExists()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            await File.WriteAllTextAsync(_testIniFilePath, "[TestSection]\nTestField=42\nTestProperty=TestValue");

            // Act
            var result = await settings.ReadAsync<MarkedClass>();

            // Assert
            Assert.Equal(42, result.TestField);
            Assert.Equal("TestValue", result.TestProperty);
        }

        [Fact]
        public async Task ReadAsync_ShouldThrowExceptionIfCacheIsEmptyAndFileCannotBeLoaded()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            File.Delete(_testIniFilePath);  // Simulate a missing file

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => settings.ReadAsync<MarkedClass>());
        }

        [Fact]
        public async Task WriteAsync_ShouldCreateBackupAndWriteData()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            var originalData = new MarkedClass { TestField = 42, TestProperty = "TestValue" };

            // Act
            await settings.WriteAsync(originalData);

            // Assert: Check if the backup file is created
            Assert.True(File.Exists(_testIniFilePath + ".bak"));

            // Assert: Check if the data is correctly written
            var fileContent = await File.ReadAllTextAsync(_testIniFilePath);
            Assert.Contains("TestField=42", fileContent);
            Assert.Contains("TestProperty=TestValue", fileContent);
        }

        [Fact] 
        public async Task ReadSafeAsync_ShouldReturnDefaultIfCacheIsEmptyOrFileIsCorrupted()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);

            // Write corrupted data to the INI file
            File.WriteAllText(_testIniFilePath, "[InvalidSection");

            // Act
            var result = await settings.ReadSafeAsync<MarkedClass>();

            // Assert: result should be a default instance of MarkedClass
            Assert.NotNull(result);
            Assert.Equal(0, result.TestField);
            Assert.Null(result.TestProperty);
        }


        [Fact]
        public async Task WriteAsync_ShouldThrowIfFileIsReadOnly()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            var testData = new MarkedClass { TestField = 42, TestProperty = "TestValue" };

            // Set file as read-only
            File.SetAttributes(_testIniFilePath, FileAttributes.ReadOnly);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => settings.WriteAsync(testData));

            // Reset file attributes
            File.SetAttributes(_testIniFilePath, FileAttributes.Normal);
        }

        [Fact]
        public async Task RestoreFromBackup_ShouldRestoreFileFromBackup()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            await File.WriteAllTextAsync(_testIniFilePath, "Original Content");
            await File.WriteAllTextAsync(_testIniFilePath + ".bak", "Backup Content");

            // Act
            settings.RestoreFromBackup();

            // Assert
            var fileContent = await File.ReadAllTextAsync(_testIniFilePath);
            Assert.Equal("Backup Content", fileContent);
        }

        [Fact]
        public void RestoreFromBackup_ShouldThrowIfBackupFileDoesNotExist()
        {
            // Arrange
            var settings = IniSettings.CreateAsync(_testIniFilePath).Result;

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => settings.RestoreFromBackup());
        }

        [Fact]
        public async Task ReadAsync_ShouldThrowIfMalformedIniFile()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            await File.WriteAllTextAsync(_testIniFilePath, "[TestSection\nKeyWithoutValue");

            // Act & Assert
            await Assert.ThrowsAsync<IOException>(() => settings.ReadAsync<MarkedClass>());
        }

        [Fact]
        public async Task WriteAsync_ShouldCorrectlyWriteEnumValues()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            var originalData = new MarkedClassWithEnum
            {
                TestEnumField = TestEnum.SecondValue,
                TestEnumProperty = TestEnum.ThirdValue
            };

            // Act
            await settings.WriteAsync(originalData);

            // Assert: Check if the enum values are correctly written
            var fileContent = await File.ReadAllTextAsync(_testIniFilePath);
            Assert.Contains("TestEnumField=SecondValue", fileContent);
            Assert.Contains("TestEnumProperty=ThirdValue", fileContent);
        }

        [Fact]
        public async Task ReadAsync_ShouldCorrectlyParseEnumValues()
        {
            // Arrange
            var settings = await IniSettings.CreateAsync(_testIniFilePath);
            await File.WriteAllTextAsync(_testIniFilePath, "[TestSection]\nTestEnumField=SecondValue\nTestEnumProperty=ThirdValue");

            // Act
            var result = await settings.ReadAsync<MarkedClassWithEnum>();

            // Assert
            Assert.Equal(TestEnum.SecondValue, result.TestEnumField);
            Assert.Equal(TestEnum.ThirdValue, result.TestEnumProperty);
        }

        // Additional classes for testing

        [IniClass("TestSection")]
        public class MarkedClass
        {
            [Ini("TestField")]
            public int TestField { get; set; }

            [Ini("TestProperty")]
            public string TestProperty { get; set; }
        }

        [IniClass("TestSection")]
        public class MarkedClassWithEnum
        {
            [Ini("TestEnumField")]
            public TestEnum TestEnumField { get; set; }

            [Ini("TestEnumProperty")]
            public TestEnum TestEnumProperty { get; set; }
        }

        public enum TestEnum
        {
            FirstValue,
            SecondValue,
            ThirdValue
        }
    }
}
