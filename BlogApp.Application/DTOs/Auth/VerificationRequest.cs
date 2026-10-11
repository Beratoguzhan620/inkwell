using System;
using System.Collections.Generic;
using System.Text;

namespace BlogApp.Application.DTOs.Auth;

public class VerificationRequest
{
    public int UserId { get; set; }
    public string Code { get; set; }
}
