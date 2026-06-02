\# 🛒 微服务电商系统（EShop.Microservices）



一个基于 \*\*.NET 9\*\* 的微服务电商 Demo，包含用户、商品、订单、支付四个独立服务，完整覆盖电商核心业务流程。  

所有数据持久化到 \*\*SQL Server\*\*，前端提供一个简易 HTML 页面，可直接与后端 API 交互。



\---



\## 系统架构



```mermaid

graph TD

&#x20;   Browser\["浏览器 (shop.html)"]

&#x20;   Gateway\["用户服务 (5173)"] -->|JWT 认证| Browser

&#x20;   UserService\["用户服务 (5173)"] -->|HttpClient| ProductService\["商品服务 (5002)"]

&#x20;   UserService -->|HttpClient| OrderService\["订单服务 (5003)"]

&#x20;   UserService -->|HttpClient| PaymentService\["支付服务 (5004)"]

&#x20;   PaymentService -->|HttpClient| OrderService

&#x20;   OrderService -->|HttpClient| ProductService



&#x20;   UserDB\["SQL Server: UserDb"] --- UserService

&#x20;   ProductDB\["SQL Server: ProductDb"] --- ProductService

&#x20;   OrderDB\["SQL Server: OrderDb"] --- OrderService

