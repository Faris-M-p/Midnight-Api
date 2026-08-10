using MidnightApi.Data;
using MidnightApi.DataAccess;
using MidnightApi.Interfaces;
using MidnightApi.Models;

namespace MidnightApi.Repositories;

public class AccessTokensRepository : IAccessTokensRepository
{
    private readonly IDataAccessDapper _dataAccessDapper;

    public AccessTokensRepository(IDataAccessDapper dataAccessDapper)
    {
        _dataAccessDapper = dataAccessDapper;
    }

    public Task<List<OutputAccessTokenItem>> GetListAsync(InputAccessTokenList input) =>
        _dataAccessDapper.GetListByStoredProcedureAsync<OutputAccessTokenItem>(
            StoredProcedures.AccessTokenList, input);

    public Task<OutputGetAccessToken?> GetByIdAsync(InputGetAccessToken input) =>
        _dataAccessDapper.GetPayloadByStoredProcedureAsync<OutputGetAccessToken>(
            StoredProcedures.AccessTokenSelect, input);

    public Task<OutputCreateAccessToken> CreateAsync(InputCreateAccessToken input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateAccessToken>(
            StoredProcedures.AccessTokenInsert, input);

    public Task<OutputUpdateAccessToken> UpdateAsync(InputUpdateAccessToken input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateAccessToken>(
            StoredProcedures.AccessTokenUpdate, input);

    public Task<OutputSetAccessTokenStatus> SetStatusAsync(InputSetAccessTokenStatus input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputSetAccessTokenStatus>(
            StoredProcedures.AccessTokenSetStatus, input);

    public Task<OutputDeleteAccessToken> SoftDeleteAsync(InputDeleteAccessToken input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputDeleteAccessToken>(
            StoredProcedures.AccessTokenDelete, input);

    public Task<List<OutputAccessTokenLoginCandidate>> ListForLoginAsync(InputAccessTokenList input) =>
        _dataAccessDapper.GetListByStoredProcedureAsync<OutputAccessTokenLoginCandidate>(
            StoredProcedures.AccessTokenListForLogin, input);

    public Task<OutputAccessTokenRecordLogin> RecordLoginAsync(InputAccessTokenRecordLogin input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccessTokenRecordLogin>(
            StoredProcedures.AccessTokenRecordLogin, input);
}
