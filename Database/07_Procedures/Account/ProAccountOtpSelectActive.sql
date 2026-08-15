CREATE OR REPLACE PROCEDURE "ProAccountOtpSelectActive"(
    "p_FK_UserAccounts" BIGINT,
    "p_Purpose" VARCHAR,
    INOUT "p_Result" REFCURSOR DEFAULT 'account_otp_active'
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
      AND o."IsUsed" = FALSE
    ORDER BY o."CreatedOn" DESC
    LIMIT 1;
END;
$$;
