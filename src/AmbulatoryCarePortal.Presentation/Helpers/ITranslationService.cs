namespace AmbulatoryCarePortal.Presentation.Helpers;

public interface ITranslationService
{
    string T(string key, params object[] args);
    string CurrentLanguage { get; }
    bool IsRtl => CurrentLanguage == "ar";
}
