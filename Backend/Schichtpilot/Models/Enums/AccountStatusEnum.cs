namespace Schichtpilot.Models.Enums;

/// <summary>
/// Contains the states a user account ca be in.
/// </summary>
public enum AccountStatusEnum
{
    None,
    EmailVerified,
    EmailNotVerified,
    Locked,
    Ok
}