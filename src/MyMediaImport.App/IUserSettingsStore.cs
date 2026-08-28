namespace MyMediaImport.App;

internal interface IUserSettingsStore
{
    AppUserSettings Load();

    void Save(AppUserSettings settings);
}
