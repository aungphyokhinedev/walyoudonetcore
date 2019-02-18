using System.ComponentModel.DataAnnotations;

public class ModelResetPassword
{
    [Required]
    [StringLength(25)]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [StringLength(60)]
    public string ResetToken { get; set; }
    [Required]
    [StringLength(25)]
    public string NewPassword { get; set; }
    [Required]
    [StringLength(25)]
    public string ConfirmPassword { get; set; }
}

