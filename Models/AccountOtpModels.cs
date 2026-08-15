using MidnightApi.DataAccess;

namespace MidnightApi.Models;

public class InputInsertAccountOtp
{
    [DbParam("p_FK_UserAccounts")]
    public long AccountId { get; set; }

    [DbParam("p_Email")]
    public string Email { get; set; } = string.Empty;

    [DbParam("p_OtpHash")]
    public string OtpHash { get; set; } = string.Empty;

    [DbParam("p_Purpose")]
    public string Purpose { get; set; } = string.Empty;

    [DbParam("p_ExpiresOn")]
    public DateTime ExpiresOn { get; set; }

    [DbParam("p_MaxAttempts")]
    public int MaxAttempts { get; set; } = 5;
}

public class InputInvalidateAccountOtp
{
    [DbParam("p_FK_UserAccounts")]
    public long AccountId { get; set; }

    [DbParam("p_Purpose")]
    public string Purpose { get; set; } = string.Empty;

    [DbParam("p_CancelledBy")]
    public string CancelledBy { get; set; } = "system";
}

public class InputGetActiveAccountOtp
{
    [DbParam("p_FK_UserAccounts")]
    public long AccountId { get; set; }

    [DbParam("p_Purpose")]
    public string Purpose { get; set; } = string.Empty;
}

public class InputAccountOtpById
{
    [DbParam("p_ID_AccountOtps")]
    public long Id { get; set; }
}

public class InputMarkAccountOtpUsed
{
    [DbParam("p_ID_AccountOtps")]
    public long Id { get; set; }

    [DbParam("p_ResetTokenHash")]
    public string? ResetTokenHash { get; set; }

    [DbParam("p_ResetTokenExpiresOn")]
    public DateTime? ResetTokenExpiresOn { get; set; }
}

public class OutputAccountOtp
{
    public long ID_AccountOtps { get; set; }
    public long FK_UserAccounts { get; set; }
    public string Email { get; set; } = string.Empty;
    public string OtpHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UsedOn { get; set; }
    public DateTime LastSentOn { get; set; }
    public string? ResetTokenHash { get; set; }
    public DateTime? ResetTokenExpiresOn { get; set; }
}

public class OutputAccountOtpWrite : CommonResponse<IdResponse>
{
}

public class OutputOtpChallenge
{
    public string Email { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public int ResendAvailableInSeconds { get; set; }
    public int ExpiresInSeconds { get; set; }
}
