CREATE OR REPLACE PROCEDURE "ProAccountOtpSelectByResetToken"(
    "p_FK_UserAccounts" BIGINT,
    "p_Purpose" VARCHAR,
    INOUT "p_Result" REFCURSOR DEFAULT 'account_otp_reset'
)
LANGUAGE plpgsql
AS $$
BEGIN
    OPEN "p_Result" FOR
    SELECT
        o."ID_AccountOtps",
        o."FK_UserAccounts",
        o."Email",
        o."OtpHash",
        o."Purpose",
        o."ExpiresOn",
        o."AttemptCount",
        o."MaxAttempts",
        o."IsUsed",
        o."CreatedOn",
        o."UsedOn",
        o."LastSentOn",
        o."ResetTokenHash",
        o."ResetTokenExpiresOn"
    FROM "AccountOtps" o
    WHERE o."FK_UserAccounts" = "p_FK_UserAccounts"
      AND o."Purpose" = "p_Purpose"
      AND o."IsCancelled" = FALSE
      AND o."IsUsed" = TRUE
      AND o."ResetTokenHash" IS NOT NULL
      AND o."ResetTokenExpiresOn" IS NOT NULL
      AND o."ResetTokenExpiresOn" > NOW()
    ORDER BY o."UsedOn" DESC NULLS LAST, o."CreatedOn" DESC
    LIMIT 1;
END;
$$;
