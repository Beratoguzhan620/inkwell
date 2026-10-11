using System;
using System.Collections.Generic;
using System.Text;

namespace BlogApp.Application.DTOs.Auth;

public class RegisterResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;
}
