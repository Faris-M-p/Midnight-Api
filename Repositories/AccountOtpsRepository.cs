using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;

namespace MidnightApi.Repositories;

public class AccountOtpsRepository : IAccountOtpsRepository
{
    private readonly IDataAccessDapper _iDataAccessDapper;

    public AccountOtpsRepository(IDataAccessDapper dataAccessDapper)
    {
        _iDataAccessDapper = dataAccessDapper;
    }

    public Task<OutputAccountOtpWrite> InsertAsync(InputInsertAccountOtp input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpInsert, input);

    public Task<OutputAccountOtpWrite> InvalidateAsync(InputInvalidateAccountOtp input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpInvalidate, input);

    public Task<OutputAccountOtp?> GetActiveAsync(InputGetActiveAccountOtp input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputAccountOtp>(
            StoredProcedures.AccountOtpSelectActive, input);

    public Task<OutputAccountOtp?> GetByIdAsync(InputAccountOtpById input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputAccountOtp>(
            StoredProcedures.AccountOtpSelectById, input);

    public Task<OutputAccountOtpWrite> IncrementAttemptAsync(InputAccountOtpById input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpIncrementAttempt, input);

    public Task<OutputAccountOtpWrite> MarkUsedAsync(InputMarkAccountOtpUsed input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpMarkUsed, input);

    public Task<OutputAccountOtp?> GetLatestResetTokenAsync(InputGetActiveAccountOtp input) =>
        _iDataAccessDapper.GetSingleOrDefaultByStoredProcedureAsync<OutputAccountOtp>(
            StoredProcedures.AccountOtpSelectByResetToken, input);

    public Task<OutputAccountOtpWrite> ClearResetTokenAsync(InputAccountOtpById input) =>
        _iDataAccessDapper.GetSingleByStoredProcedureAsync<OutputAccountOtpWrite>(
            StoredProcedures.AccountOtpClearResetToken, input);
}
