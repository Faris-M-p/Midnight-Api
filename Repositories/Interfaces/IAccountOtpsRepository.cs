using MidnightApi.Models;

namespace MidnightApi.Repositories.Interfaces;

public interface IAccountOtpsRepository
{
    Task<OutputAccountOtpWrite> InsertAsync(InputInsertAccountOtp input);
    Task<OutputAccountOtpWrite> InvalidateAsync(InputInvalidateAccountOtp input);
    Task<OutputAccountOtp?> GetActiveAsync(InputGetActiveAccountOtp input);
    Task<OutputAccountOtp?> GetByIdAsync(InputAccountOtpById input);
    Task<OutputAccountOtpWrite> IncrementAttemptAsync(InputAccountOtpById input);
    Task<OutputAccountOtpWrite> MarkUsedAsync(InputMarkAccountOtpUsed input);
    Task<OutputAccountOtp?> GetLatestResetTokenAsync(InputGetActiveAccountOtp input);
    Task<OutputAccountOtpWrite> ClearResetTokenAsync(InputAccountOtpById input);
}
