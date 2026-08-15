CREATE OR REPLACE PROCEDURE "ProAccountOtpSelectById"(
    "p_ID_AccountOtps" BIGINT,
    INOUT "p_Result" REFCURSOR DEFAULT 'account_otp_by_id'
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
    WHERE o."ID_AccountOtps" = "p_ID_AccountOtps"
      AND o."IsCancelled" = FALSE;
END;
$$;
