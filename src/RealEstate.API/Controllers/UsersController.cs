using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RealEstate.Core.DTOs;
using RealEstate.Core.Entities;

namespace RealEstate.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UsersController(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            var userProfileDto = _mapper.Map<UserProfileDto>(user);
            return Ok(userProfileDto);
        }

        [HttpPut("profile")]
        public async Task<ActionResult<UserProfileUpdateResponseDto>> UpdateUserProfile(UserProfileUpdateDto profileUpdateDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            // Only update email if provided
            if (!string.IsNullOrEmpty(profileUpdateDto.Email))
            {
                // Check if email is being changed and if it's already in use
                if (user.Email != profileUpdateDto.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(profileUpdateDto.Email);
                    if (existingUser != null && existingUser.Id != userId)
                        return BadRequest(new { message = "البريد الإلكتروني مستخدم بالفعل" });

                    user.Email = profileUpdateDto.Email;
                    user.NormalizedEmail = profileUpdateDto.Email.ToUpper();
                }
            }

            // Only update FullName if provided
            if (!string.IsNullOrEmpty(profileUpdateDto.FullName))
            {
                user.FullName = profileUpdateDto.FullName;
            }

            // Only update PhoneNumber if provided
            if (!string.IsNullOrEmpty(profileUpdateDto.PhoneNumber))
            {
                user.PhoneNumber = profileUpdateDto.PhoneNumber;
            }

            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            var updatedProfileDto = _mapper.Map<UserProfileDto>(user);

            return Ok(new UserProfileUpdateResponseDto
            {
                Success = true,
                Message = "تم تحديث الملف الشخصي بنجاح",
                Profile = updatedProfileDto
            });
        }

        [HttpPut("change-password")]
        public async Task<ActionResult<ChangePasswordResponseDto>> ChangePassword(ChangePasswordDto changePasswordDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "المستخدم غير مصرح له" });

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            // Check current password
            if (!await _userManager.CheckPasswordAsync(user, changePasswordDto.CurrentPassword))
                return BadRequest(new { message = "كلمة المرور الحالية غير صحيحة" });

            var result = await _userManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new ChangePasswordResponseDto
            {
                Success = true,
                Message = "تم تغيير كلمة المرور بنجاح"
            });
        }
    }
}