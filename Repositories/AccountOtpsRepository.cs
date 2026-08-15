using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;

namespace MidnightApi.Repositories;

public class AccountOtpsRepository : IAccountOtpsRepository
{
    private readonly IDataAccessDapper _dataAccessDapper;

    public AccountOtpsRepository(IDataAccessDapper dataAccessDapper)
    {
        _dataAccessDapper = dataAccessDapper;
    }

    public Task<OutputAccountOtpWrite> InsertAsync(InputInsertAccountOtp input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpInsert, input);

    public Task<OutputAccountOtpWrite> InvalidateAsync(InputInvalidateAccountOtp input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpInvalidate, input);

    public Task<OutputAccountOtp?> GetActiveAsync(InputGetActiveAccountOtp input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputAccountOtp>(
            StoredProcedures.AccountOtpSelectActive, input);

    public Task<OutputAccountOtp?> GetByIdAsync(InputAccountOtpById input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputAccountOtp>(
            StoredProcedures.AccountOtpSelectById, input);

    public Task<OutputAccountOtpWrite> IncrementAttemptAsync(InputAccountOtpById input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpIncrementAttempt, input);

    public Task<OutputAccountOtpWrite> MarkUsedAsync(InputMarkAccountOtpUsed input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpMarkUsed, input);

    public Task<OutputAccountOtp?> GetLatestResetTokenAsync(InputGetActiveAccountOtp input) =>
        _dataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputAccountOtp>(
            StoredProcedures.AccountOtpSelectByResetToken, input);

    public Task<OutputAccountOtpWrite> ClearResetTokenAsync(InputAccountOtpById input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpClearResetToken, input);
}
