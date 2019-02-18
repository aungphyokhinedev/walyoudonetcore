using System.ComponentModel.DataAnnotations;

public class ModelLoginCredentials
{
    [Required]
    [StringLength(25)]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [StringLength(25)]
    public string Password { get; set; }
}


public class ModelTokenCredentials
{
    [Required]
    public string Token { get; set; }
}