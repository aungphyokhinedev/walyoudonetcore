using System;
using System.ComponentModel.DataAnnotations;

public class ModelUserCredentials
{
    public int UserID { get; set; }
    [Required]
    [StringLength(25)]
    [EmailAddress]
    public string Email { get; set; }
    [StringLength(25)]
    [Phone]
    public string MobileNo { get; set; }
    [Required]
    [StringLength(25)]
    public string Password { get; set; }
    [Required]
    [StringLength(25)]
    
    public string ConfirmPassword { get; set; }

    public string Salt { get; set; }
    public string Token { get; set; }
    public bool Locked { get; set; }
    public int LoginAttempt { get; set; }
    public bool Disabled { get; set; }

    public bool Verified { get; set; }

    public string VerificationCode { get; set; }

    public DateTime CreateDate  { get; set; }
    public DateTime CreatLastLoginDate  { get; set; }
    public DateTime LastPasswordChange  { get; set; }

    public int UserType { get; set; }
}
