using System.Collections.Immutable;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

public interface ILanguageProvider
{
    public ImmutableList<MatroskaLanguageOption> Languages { get; }

    public void LoadLanguages();
}
