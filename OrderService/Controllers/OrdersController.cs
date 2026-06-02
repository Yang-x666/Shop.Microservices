using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using System;
using System.Threading.Tasks;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public OrdersController(OrderDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 创建订单
    /// POST /api/orders
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // 1. 调用商品服务获取商品信息
        var client = _httpClientFactory.CreateClient();
        var productResponse = await client.GetAsync($"http://localhost:5002/api/products/{request.ProductId}");

        if (!productResponse.IsSuccessStatusCode)
            return BadRequest(new { message = "商品不存在" });

        var product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
        if (product == null)
            return BadRequest(new { message = "商品不存在" });

        // 2. 检查库存
        if (product.Stock < request.Quantity)
            return BadRequest(new { message = "库存不足" });

        // 3. 扣减库存（调用商品服务）
        var stockRequest = new { Stock = product.Stock - request.Quantity };
        var stockResponse = await client.PutAsJsonAsync(
            $"http://localhost:5002/api/products/{request.ProductId}/stock", stockRequest);

        if (!stockResponse.IsSuccessStatusCode)
            return StatusCode(500, new { message = "扣减库存失败" });

        // 4. 创建订单
        var order = new Order
        {
            UserId = request.UserId,
            ProductId = request.ProductId,
            ProductName = product.Name,
            Quantity = request.Quantity,
            UnitPrice = product.Price,
            Status = "待支付"
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "下单成功",
            orderId = order.Id,
            totalPrice = order.TotalPrice
        });
    }

    /// <summary>
    /// 获取用户的所有订单
    /// GET /api/orders/user/{userId}
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserOrders(Guid userId)
    {
        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(orders);
    }
    /// <summary>
    /// 支付确认（内部调用）
    /// PUT /api/orders/{id}/pay
    /// </summary>
    [HttpPut("{id}/pay")]
    public async Task<IActionResult> PayOrder(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "订单不存在" });

        if (order.Status == "已支付")
            return BadRequest(new { message = "订单已支付，请勿重复支付" });

        if (order.Status == "已取消")
            return BadRequest(new { message = "订单已取消，无法支付" });

        order.Status = "已支付";
        await _db.SaveChangesAsync();

        return Ok(new { message = "支付成功", orderId = order.Id, status = order.Status });
    }
}

// 请求格式
public class CreateOrderRequest
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

// 商品服务返回格式
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
