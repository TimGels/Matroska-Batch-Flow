using MatroskaBatchFlow.Core.Models;
using System.Collections.Immutable;

namespace MatroskaBatchFlow.Core.Services
{
    public interface ILanguageProvider
    {
        public ImmutableList<MatroskaLanguageOption> Languages { get; }

        public void LoadLanguages();
    }
}
