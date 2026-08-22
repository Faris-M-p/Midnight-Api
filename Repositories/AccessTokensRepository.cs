using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;

namespace MidnightApi.Repositories;

public class AccessTokensRepository : IAccessTokensRepository
{
    private readonly IDataAccessDapper _iDataAccessDapper;

    public AccessTokensRepository(IDataAccessDapper dataAccessDapper)
    {
        _iDataAccessDapper = dataAccessDapper;
    }

    public Task<List<OutputAccessTokenItem>> GetListAsync(InputAccessTokenList input) =>
        _iDataAccessDapper.GetListByStoredProcedureAsync<OutputAccessTokenItem>(
            StoredProcedures.AccessTokenList, input);

    public Task<OutputGetAccessToken?> GetByIdAsync(InputGetAccessToken input) =>
        _iDataAccessDapper.GetPayloadByStoredProcedureAsync<OutputGetAccessToken>(
            StoredProcedures.AccessTokenSelect, input);

    public Task<OutputCreateAccessToken> CreateAsync(InputCreateAccessToken input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateAccessToken>(
            StoredProcedures.AccessTokenInsert, input);

    public Task<OutputUpdateAccessToken> UpdateAsync(InputUpdateAccessToken input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccessToken>(
            StoredProcedures.AccessTokenUpdate, input);

    public Task<OutputSetAccessTokenStatus> SetStatusAsync(InputSetAccessTokenStatus input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputSetAccessTokenStatus>(
            StoredProcedures.AccessTokenSetStatus, input);

    public Task<OutputDeleteAccessToken> SoftDeleteAsync(InputDeleteAccessToken input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputDeleteAccessToken>(
            StoredProcedures.AccessTokenDelete, input);

    public Task<List<OutputAccessTokenLoginCandidate>> ListForLoginAsync(InputAccessTokenList input) =>
        _iDataAccessDapper.GetListByStoredProcedureAsync<OutputAccessTokenLoginCandidate>(
            StoredProcedures.AccessTokenListForLogin, input);

    public Task<OutputAccessTokenRecordLogin> RecordLoginAsync(InputAccessTokenRecordLogin input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccessTokenRecordLogin>(
            StoredProcedures.AccessTokenRecordLogin, input);
}
