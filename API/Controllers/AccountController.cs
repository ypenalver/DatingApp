using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;


public class AccountController (DataContext context, ITokenService tokenService, IMapper mapper): BaseApiController
{
    [HttpPost("register")] //account/register
    public async Task<ActionResult<UserDto>>Register(RegisterDto registerDto)
    {
        
        if( await UserExists (registerDto.UserName)) return BadRequest("UserName is taken");
        
        using var hmac = new HMACSHA512();

        var user = mapper.Map<AppUser>(registerDto);

        user.UserName = registerDto.UserName.ToLower();
        user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
        user.PasswordSalt = hmac.Key;
       
         context.Users.Add(user);
         await context.SaveChangesAsync();

         return new UserDto
         {
             UserName = user.UserName,
             Token = tokenService.CreateToken(user),
             KnownAs = user.KnownAs
         };
    }
  [HttpPost("login")] 
    public async Task<ActionResult<UserDto>>Login(LoginDto loginDto)
    {
        
        var user = await context.Users
             .Include(p => p.Photos)
                .FirstOrDefaultAsync(x =>
                    x.UserName == loginDto.UserName.ToLower());
        
           
        if (user == null) return Unauthorized("Invalid username");
       
       using var hmac = new HMACSHA512(user.PasswordSalt);

       var ComputeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

       for (int i = 0; i < ComputeHash.Length; i++)
       {
            if(ComputeHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
       }
       
       return new UserDto
        {
            UserName = user.UserName,
            KnownAs = user.KnownAs,
            Token = tokenService.CreateToken(user),
            PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
        };

    }
    

    private async Task<bool> UserExists(string username)
    {
        return await context.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower());
    }
    
}