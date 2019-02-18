using System.ComponentModel.DataAnnotations;

public class ModelChangePassword
{
    [Required]
    [StringLength(25)]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [StringLength(25)]
    public string CurrentPassword { get; set; }
    [Required]
    [StringLength(25)]
    public string NewPassword { get; set; }
    [Required]
    [StringLength(25)]
    public string ConfirmPassword { get; set; }
}

