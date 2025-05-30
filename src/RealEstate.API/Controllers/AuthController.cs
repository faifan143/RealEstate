using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Core.DTOs;
using RealEstate.Core.Entities;
using RealEstate.Core.Interfaces;

namespace RealEstate.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            // Check if phone number already exists
            var existingUser = await _userManager.FindByNameAsync(registerDto.PhoneNumber);
            if (existingUser != null)
                return BadRequest(new { message = "رقم الهاتف مستخدم بالفعل" });

            var user = new ApplicationUser
            {
                UserName = registerDto.PhoneNumber,
                PhoneNumber = registerDto.PhoneNumber,
                FullName = registerDto.FullName,
                Email = registerDto.Email,
                CreatedAt = DateTime.UtcNow,
                PhoneNumberConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"فشل في إنشاء الحساب: {errors}" });
            }

            var token = await _tokenService.GenerateTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty
                }
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(PhoneLoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.PhoneNumber);
            if (user == null)
                return BadRequest(new { message = "رقم الهاتف أو كلمة المرور غير صحيحة" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (!result.Succeeded)
                return BadRequest(new { message = "رقم الهاتف أو كلمة المرور غير صحيحة" });

            var token = await _tokenService.GenerateTokenAsync(user);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user);

            return Ok(new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    ProfilePictureUrl = user.ProfilePictureUrl ?? string.Empty
                }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByNameAsync(forgotPasswordDto.PhoneNumber);
            if (user == null)
                return BadRequest(new { message = "المستخدم غير موجود" });

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // In a real application, you would send this token to the user via SMS
            // For development, we'll return it directly
            return Ok(new { 
                message = "تم إرسال رمز إعادة تعيين كلمة المرور",
                resetToken = token // Remove this in production
            });
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByNameAsync(resetPasswordDto.PhoneNumber);
            if (user == null)
                return BadRequest(new { message = "المستخدم غير موجود" });

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordDto.ResetToken, resetPasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"فشل في إعادة تعيين كلمة المرور: {errors}" });
            }

            return Ok(new { message = "تم إعادة تعيين كلمة المرور بنجاح" });
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto refreshTokenDto)
        {
            var result = await _tokenService.RefreshTokenAsync(refreshTokenDto.RefreshToken);
            if (result == null)
                return BadRequest(new { message = "الرمز المميز غير صالح" });

            return Ok(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return BadRequest(new { message = "المستخدم غير موجود" });

            var result = await _userManager.ChangePasswordAsync(user, 
                changePasswordDto.CurrentPassword, 
                changePasswordDto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"فشل في تغيير كلمة المرور: {errors}" });
            }

            return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await _tokenService.RevokeAllRefreshTokensAsync(userId);
            }

            return Ok(new { message = "تم تسجيل الخروج بنجاح" });
        }
    }
}
