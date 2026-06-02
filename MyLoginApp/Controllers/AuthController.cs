using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyLoginApp.Data;
using MyLoginApp.Models;

namespace MyLoginApp.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    /// <summary>
    /// 注册：POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == request.Username);
        if (exists)
            return BadRequest(new { message = "用户名已存在" });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "密码至少6位" });

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 注册成功直接返回 Token
        var token = GenerateToken(user);
        return Ok(new { message = "注册成功", token, userId = user.Id });
    }

    /// <summary>
    /// 登录：POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null)
            return BadRequest(new { message = "用户名或密码错误" });

        bool correct = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!correct)
            return BadRequest(new { message = "用户名或密码错误" });

        // 登录成功返回 Token
        var token = GenerateToken(user);
        return Ok(new { message = $"欢迎回来，{user.Username}！", token, userId = user.Id, email = user.Email });
    }

    /// <summary>
    /// 获取商品列表（需要登录）
    /// GET /api/auth/products
    /// </summary>
    [HttpGet("products")]
    [Authorize]  // ← 加了这行，必须带 Token 才能访问
    public async Task<IActionResult> GetProducts([FromServices] IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync("http://localhost:5002/api/products");

        if (!response.IsSuccessStatusCode)
        {
            return new ObjectResult(new { message = "获取商品列表失败" }) { StatusCode = 500 };
        }

        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult { Content = content, ContentType = "application/json", StatusCode = 200 };
    }

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    private string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpirationMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    /// <summary>
    /// 测试 Token 是否有效（需要登录）
    /// GET /api/auth/test
    /// </summary>
    [HttpGet("test")]
    [Authorize]
    public IActionResult TestToken()
    {
        var userId = User.FindFirst("userId")?.Value;
        var username = User.Identity?.Name;
        return Ok(new { message = "Token有效", userId, username });
    }
    /// <summary>
    /// 下单（需要登录）
    /// POST /api/auth/orders/create
    /// </summary>
    [HttpPost("orders/create")]
    [Authorize]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest orderRequest,
        [FromServices] IHttpClientFactory httpClientFactory)
    {
        // 从 Token 中获取当前用户 ID
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "无效的Token" });

        var client = httpClientFactory.CreateClient();

        // 调用订单服务，传入用户 ID
        var payload = new
        {
            UserId = userId,
            orderRequest.ProductId,
            orderRequest.Quantity
        };

        var response = await client.PostAsJsonAsync("http://localhost:5003/api/orders", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }

        var result = await response.Content.ReadAsStringAsync();
        return Content(result, "application/json");
    }
    /// <summary>
    /// 支付订单（需要登录）
    /// POST /api/auth/orders/pay
    /// </summary>
    [HttpPost("orders/pay")]
    [Authorize]
    public async Task<IActionResult> PayOrder(
        [FromBody] PayOrderRequest payRequest,
        [FromServices] IHttpClientFactory httpClientFactory)
    {
        var client = httpClientFactory.CreateClient();

        // 调用支付服务
        var response = await client.PostAsJsonAsync("http://localhost:5004/api/payments",
            new { payRequest.OrderId });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, error);
        }

        var result = await response.Content.ReadAsStringAsync();
        return Content(result, "application/json");
    }
}

// 请求格式
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
public class PayOrderRequest
{
    public Guid OrderId { get; set; }
}
public class CreateOrderRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

