using TeleBotService.Core.Model;

namespace TeleBotService.Data;

public interface IInternetRadioRepository
{
    Task<RadioDiscoverResponse.ResultData.Stream?> GetStreamData(string radioId);

    Task<RadioDiscoverResponse.ResultData.Stream?> SaveStreamData(string radioId,
        RadioDiscoverResponse.ResultData.Stream? streamData);

    Task<Dictionary<string, RadioDiscoverResponse.ResultData.Stream>?> ListStreamData();
}
