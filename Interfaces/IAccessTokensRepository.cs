using MidnightApi.Models;

namespace MidnightApi.Interfaces;

public interface IAccessTokensRepository
{
    Task<List<OutputAccessTokenItem>> GetListAsync(InputAccessTokenList input);
    Task<OutputGetAccessToken?> GetByIdAsync(InputGetAccessToken input);
    Task<OutputCreateAccessToken> CreateAsync(InputCreateAccessToken input);
    Task<OutputUpdateAccessToken> UpdateAsync(InputUpdateAccessToken input);
    Task<OutputSetAccessTokenStatus> SetStatusAsync(InputSetAccessTokenStatus input);
    Task<OutputDeleteAccessToken> SoftDeleteAsync(InputDeleteAccessToken input);
    Task<List<OutputAccessTokenLoginCandidate>> ListForLoginAsync(InputAccessTokenList input);
    Task<OutputAccessTokenRecordLogin> RecordLoginAsync(InputAccessTokenRecordLogin input);
}
