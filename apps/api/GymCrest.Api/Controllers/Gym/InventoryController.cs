using GymCrest.Contracts.Gym.Inventory;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Common;
using GymCrest.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Filters;
using GymCrest.Shared.Enums;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/inventory")]
    [Authorize(Roles = "GymOwner,Staff")]
    [RequireModuleAccess("inventory")]
    [ApiController]
    public class InventoryController : BaseApiController
    {
        private readonly IInventoryService _inventoryService;

        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<InventoryStatsDto>> GetInventoryStats()
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _inventoryService.GetInventoryStatsAsync(GymId.Value, SecureBranchId));
        }

        // --- Products ---

        [HttpGet("products")]
        public async Task<ActionResult<List<InventoryItemDto>>> GetProducts([FromQuery] PaginationParams pagination)
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _inventoryService.GetProductsAsync(GymId.Value, pagination, SecureBranchId));
        }

        [HttpPost("products")]
        public async Task<ActionResult<InventoryItemDto>> AddProduct([FromBody] CreateProductDto dto)
        {
            if (GymId == null) return Unauthorized();
            InventoryItemDto result = await _inventoryService.AddProductAsync(dto, GymId.Value);
            return Ok(result);
        }

        [AuthorizeResource(OwnedResource.InventoryItem)]
        [HttpPut("products/{id}")]
        public async Task<ActionResult<InventoryItemDto>> UpdateProduct(Guid id, [FromBody] CreateProductDto dto)
        {
            InventoryItemDto? result = await _inventoryService.UpdateProductAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [AuthorizeResource(OwnedResource.InventoryItem)]
        [HttpDelete("products/{id}")]
        public async Task<ActionResult> DeleteProduct(Guid id)
        {
            await _inventoryService.DeleteProductAsync(id);
            return Ok(new { message = "Product deleted successfully" });
        }

        // --- Sales ---

        [HttpPost("sales")]
        public async Task<ActionResult> RecordSale([FromBody] RecordSaleDto dto)
        {
            if (GymId == null) return Unauthorized();
            bool success = await _inventoryService.RecordSaleAsync(dto, GymId.Value);
            if (!success) return BadRequest(new { message = "Sale failed. Check stock availability." });
            return Ok(new { message = "Sale recorded successfully" });
        }

        [HttpGet("sales/history")]
        public async Task<ActionResult<PagedResponse<SaleTransactionDto>>> GetSalesHistory([FromQuery] PaginationParams pagination)
        {
            if (GymId == null) return Unauthorized();
            return Ok(await _inventoryService.GetSalesHistoryAsync(GymId.Value, pagination, SecureBranchId));
        }

        [AuthorizeResource(OwnedResource.SaleTransaction)]
        [HttpPost("sales/{id}/receipt")]
        public async Task<ActionResult> SendReceipt(Guid id)
        {
            bool success = await _inventoryService.SendSaleReceiptAsync(id);
            if (!success) return BadRequest(new { message = "Failed to send receipt email." });
            return Ok(new { message = "Receipt email sent successfully." });
        }
    }
}
