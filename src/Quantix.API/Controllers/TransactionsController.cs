using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quantix.Core.DTOs;
using Quantix.Core.Interfaces;

namespace Quantix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)!;


    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _transactionService.GetUserTransactionsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _transactionService.GetByIdAsync(id, GetUserId());
        return result.Success ? Ok(result) : NotFound(result);
    }


    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        var result = await _transactionService.CreateAsync(dto, GetUserId());
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result) : BadRequest(result);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTransactionDto dto)
    {
        var result = await _transactionService.UpdateAsync(id, dto, GetUserId());
        return result.Success ? Ok(result) : NotFound(result);
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _transactionService.DeleteAsync(id, GetUserId());
        return result.Success ? Ok(result) : NotFound(result);
    }
}
