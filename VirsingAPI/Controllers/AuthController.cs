using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VirsignAPI.ContextDB;
using VirsignAPI.ContextDB.Models;
using static System.String;

namespace VirsignAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MongoDBContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(MongoDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Регистрация пользователя
        [HttpPost("register")]
        public IActionResult Register([FromBody] UserModel user, CancellationToken ct = default)
        {
            try
            {
                if (_context.UserModel.Find(u => u.Username == user.Username).Any(ct))
                {
                    Log.Error(
                        $"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name}" +
                        $" [{nameof(UserModel)}] The user: {user.Username} already exists!");
                    return BadRequest($"[{nameof(UserModel)}] The user: {user.Username} already exists!");
                }

                user.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                _context.UserModel.InsertOne(user, cancellationToken: ct);
                return Ok(new { Message = $"User: {user.Username} registered successfully!" });
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
                return StatusCode(500, ex);
            }
        }

        // Аутентификация пользователя
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserModel user, CancellationToken ct = default )
        {
            try
            {
                var existingUser = _context.UserModel.Find(u => u.Username == user.Username && u.Password == user.Password).FirstOrDefault(ct);
                if (existingUser == null)
                {
                    Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {nameof(UserModel)} not found!");
                    return Unauthorized($"{nameof(UserModel)} not found!");
                }

                var token = GenerateJwtToken(existingUser);
                if (IsNullOrEmpty(token))
                {
                    Log.Warning($"[{nameof(GenerateJwtToken)}] token Is Null Or Empty!");
                    return NotFound();
                }
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
                return StatusCode(500, ex);
            }
        }

        private string GenerateJwtToken(UserModel user)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("Jwt");
                var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Role, user.Role)
                    }),
                    Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"])),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = jwtSettings["Issuer"],
                    Audience = jwtSettings["Audience"]
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                Log.Error($"{GetType().Name}.{new StackTrace(false).GetFrame(0)?.GetMethod()?.Name} {ex.Message}");
                return String.Empty;
            }
        }
    }
}