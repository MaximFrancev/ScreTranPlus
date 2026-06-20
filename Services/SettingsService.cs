using System.IO;
using Newtonsoft.Json;

namespace ScreTranPlus;

/// <summary>
/// Service responsible for Settings load/saving.
/// </summary>
public class SettingsService : ISettingsService
{
    /// <summary>
    /// Settings folder.
    /// </summary>
    private readonly string _path;

    /// <summary>
    /// Settings file name.
    /// </summary>
    private readonly string _filename;

    /// <summary>
    /// App settings.
    /// </summary>
    public SettingsModel Settings
    {
        get;
    }

    public SettingsService()
    {
        _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreTranPlus");
        _filename = Path.Combine(_path, "settings.json");
        Settings = Load();
    }

    /// <summary>
    /// Loads object from file.
    /// </summary>
    /// <returns>Filled settings object.</returns>
    private SettingsModel Load()
    {
        // Если файла нет - создаем новые настройки и заполняем дефолтными значениями
        if (!File.Exists(_filename))
        {
            var defaultSettings = new SettingsModel();
            defaultSettings.ResetToDefault();
            return defaultSettings;
        }

        var json = File.ReadAllText(_filename);
        if (string.IsNullOrEmpty(json))
        {
            var defaultSettings = new SettingsModel();
            defaultSettings.ResetToDefault();
            return defaultSettings;
        }

        // Возвращаем стандартную безопасную десериализацию без Replace
        var settings = JsonConvert.DeserializeObject<SettingsModel>(json);

        if (settings == null)
            return new SettingsModel();

        // Защита: если после загрузки список окон пуст, добавляем хотя бы одно окно
        if (settings.SelectionWindowPositions == null || settings.SelectionWindowPositions.Count == 0)
        {
            settings.SelectionWindowPositions = new System.Collections.ObjectModel.ObservableCollection<WindowPositionModel>
            {
                new WindowPositionModel()
            };
        }

        return settings;
    }

    /// <summary>
    /// Saves object to file.
    /// </summary>
    /// <param name="obj">Object to save.</param>
    public void Save(object obj)
    {
        // Serialize to JSON.
        var serializedSettings = JsonConvert.SerializeObject(obj, Formatting.Indented);

        // Create path.
        if (!Directory.Exists(_path))
            Directory.CreateDirectory(_path);

        // Write to file.
        var settingsFile = File.CreateText(_filename);
        settingsFile.Write(serializedSettings);
        settingsFile.Close();
    }
}