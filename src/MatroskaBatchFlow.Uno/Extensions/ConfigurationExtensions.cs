using MatroskaBatchFlow.Core.Attributes;
using MatroskaBatchFlow.Core.Models.AppSettings;
using Microsoft.Extensions.Configuration;

namespace MatroskaBatchFlow.Uno.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to configure application options.
/// </summary>
public static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers and configures all application options with data annotation validation.
        /// </summary>
        /// <param name="configuration">The configuration root containing the option values.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// All option types registered here must be decorated with <see cref="ValidatedOptionsAttribute"/>
        /// to be included in startup validation via <see cref="Utilities.ConfigurationValidator"/>.
        /// </remarks>
        public IServiceCollection AddAppConfiguration(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<LanguageOptions>()
                .Bind(configuration.GetSection(nameof(LanguageOptions)))
                .ValidateDataAnnotations();

            services.AddOptions<ScanOptions>()
                .Bind(configuration.GetSection(nameof(ScanOptions)))
                .ValidateDataAnnotations();

            services.AddOptions<AppConfigOptions>()
                .Bind(configuration.GetSection(nameof(AppConfigOptions)))
                .ValidateDataAnnotations();

            services.AddOptions<LoggingOptions>()
                .Bind(configuration.GetSection(nameof(LoggingOptions)))
                .ValidateDataAnnotations();

            return services;
        }
    }
}
