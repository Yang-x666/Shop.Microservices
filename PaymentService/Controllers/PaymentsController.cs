using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public PaymentsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// 模拟支付
    /// POST /api/payments
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Pay([FromBody] PayRequest request)
    {
        // 1. 模拟调用银行支付接口（这里直接成功）
        // 实际项目中会对接支付宝/微信等

        // 2. 调用订单服务，修改订单状态为“已支付”
        var client = _httpClientFactory.CreateClient();
        var response = await client.PutAsync(
            $"http://localhost:5003/api/orders/{request.OrderId}/pay", null);

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
public class PayRequest
{
    public Guid OrderId { get; set; }
}