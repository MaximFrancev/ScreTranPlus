using System.Collections.Generic;

namespace ScreTranPlus;

public interface ITranslationService
{
    string Translate(string input, Enumerations.Translator translator, Enumerations.TargetLanguage targetLanguage);

    string TranslateVision(List<byte[]> images, Enumerations.TargetLanguage targetLanguage);
}