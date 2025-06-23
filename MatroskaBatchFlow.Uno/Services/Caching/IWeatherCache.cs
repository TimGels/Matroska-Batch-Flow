namespace MatroskaBatchFlow.Uno.Services.Caching;
using WeatherForecast = MatroskaBatchFlow.Uno.DataContracts.WeatherForecast;
public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
