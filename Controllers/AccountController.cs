using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;
using MidnightApi.Services;
using System.Security.Cryptography;
using System.Text;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly IUserAccountsRepository _accounts;
    private readonly IMembersRepository _members;
    private readonly MemberValidationService _validation;

    public AccountController(
        IUserAccountsRepository accounts,
        IMembersRepository members,
        MemberValidationService validation)
    {
        _accounts = accounts;
        _members = members;
        _validation = validation;
    }

    [HttpGet]
    public async Task<ActionResult<List<OutputGetAccount>>> GetAll()
    {
        return Ok(await _accounts.GetAllAsync());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OutputGetAccount>> GetById(long id)
    {
        var item = await _accounts.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound(new { message = "Account not found." });
        }

        return Ok(item);
    }

    [HttpGet("member/{memberId:long}")]
    public async Task<ActionResult<OutputGetAccount>> GetByMember(long memberId)
    {
        var item = await _accounts.GetByMemberIdAsync(memberId);
        if (item is null)
        {
            return NotFound(new { message = "Account not found." });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<OutputGetAccount>> Create([FromBody] InputCreateAccountView view)
    {
        var input = MapToCreateAccountInput(view);
        if (await _members.GetByIdAsync(input.FK_Members) is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        if (await _accounts.GetByMemberIdAsync(input.FK_Members) is not null)
        {
            return Conflict(new { message = "Member already has an account." });
        }

        if (await _accounts.ExistsByUsernameAsync(input.Username))
        {
            return Conflict(new { message = "Username already exists." });
        }

        var emailError = _validation.ValidateEmail(input.Email);
        if (emailError is not null)
        {
            return BadRequest(new { message = emailError });
        }

        var created = await _accounts.CreateAsync(input, "system");
        return CreatedAtAction(nameof(GetById), new { id = created.ID_UserAccounts }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<OutputGetAccount>> Update(long id, [FromBody] InputUpdateAccountView view)
    {
        var input = MapToUpdateAccountInput(view);
        if (await _accounts.ExistsByUsernameAsync(input.Username, id))
        {
            return Conflict(new { message = "Username already exists." });
        }

        var emailError = _validation.ValidateEmail(input.Email);
        if (emailError is not null)
        {
            return BadRequest(new { message = emailError });
        }

        var updated = await _accounts.UpdateAsync(id, input, "system");

        if (updated is null)
        {
            return NotFound(new { message = "Account not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _accounts.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Account not found." });
        }

        return NoContent();
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    private static InputCreateAccount MapToCreateAccountInput(InputCreateAccountView view) => new()
    {
        FK_Members = view.FK_Members,
        Username = view.Username,
        Email = view.Email,
        PasswordHash = HashPassword(view.Password),
        IsActive = view.IsActive
    };

    private static InputUpdateAccount MapToUpdateAccountInput(InputUpdateAccountView view) => new()
    {
        Username = view.Username,
        Email = view.Email,
        PasswordHash = string.IsNullOrWhiteSpace(view.Password) ? null : HashPassword(view.Password),
        IsActive = view.IsActive
    };
}
