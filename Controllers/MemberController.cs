using Microsoft.AspNetCore.Mvc;
using MidnightApi.Interfaces;
using MidnightApi.Services;

namespace MidnightApi.Controllers;

using MidnightApi.Models.Api;

[ApiController]
[Route("api/members")]
public class MemberController : ControllerBase
{
    private readonly IMembersRepository _members;
    private readonly IFamiliesRepository _families;
    private readonly IMemberAddressesRepository _addresses;
    private readonly IMemberImagesRepository _images;
    private readonly IMemberEventsRepository _events;
    private readonly IMemberSocialLinksRepository _socialLinks;
    private readonly IMemberNotesRepository _notes;
    private readonly MemberValidationService _validation;

    public MemberController(
        IMembersRepository members,
        IFamiliesRepository families,
        IMemberAddressesRepository addresses,
        IMemberImagesRepository images,
        IMemberEventsRepository events,
        IMemberSocialLinksRepository socialLinks,
        IMemberNotesRepository notes,
        MemberValidationService validation)
    {
        _members = members;
        _families = families;
        _addresses = addresses;
        _images = images;
        _events = events;
        _socialLinks = socialLinks;
        _notes = notes;
        _validation = validation;
    }

    [HttpGet]
    public async Task<ActionResult<List<OutputGetMember>>> GetAll()
    {
        return Ok(await _members.GetAllAsync());
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<OutputGetMember>>> Search([FromQuery] InputSearchMembersView view)
    {
        if (string.IsNullOrWhiteSpace(view.Name))
        {
            return BadRequest(new { message = "Search name is required." });
        }

        return Ok(await _members.SearchByNameAsync(new InputSearchMembers
        {
            Name = view.Name,
            FamilyId = view.FamilyId
        }));
    }

    [HttpGet("tree/{familyId:long}")]
    public async Task<ActionResult<OutputGetMemberTree>> GetTree(long familyId)
    {
        if (await _families.GetByIdAsync(familyId) is null)
        {
            return NotFound(new { message = "Family not found." });
        }

        var root = await _members.GetRootWithTreeDataAsync(new InputGetMemberTree { FamilyId = familyId });
        if (root is null)
        {
            return NotFound(new { message = "Root member not found." });
        }

        return Ok(root);
    }

    [HttpGet("root/{familyId:long}")]
    public async Task<ActionResult<OutputGetMember>> GetRoot(long familyId)
    {
        var root = await _members.GetRootByFamilyIdAsync(familyId);
        if (root is null)
        {
            return NotFound(new { message = "Root member not found." });
        }

        return Ok(root);
    }

    [HttpGet("profile/{memberId:long}")]
    public async Task<ActionResult<OutputGetMemberProfile>> GetProfile(long memberId)
    {
        var member = await _members.GetByIdWithDetailsAsync(memberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        return Ok(member);
    }

    [HttpGet("family/{familyId:long}")]
    public async Task<ActionResult<List<OutputGetMember>>> GetByFamily(long familyId)
    {
        return Ok(await _members.GetByFamilyIdAsync(new InputGetFamilyMembers { FamilyId = familyId }));
    }

    [HttpGet("generation/{familyId:long}/{level:int}")]
    public async Task<ActionResult<List<OutputGetMember>>> GetByGeneration(long familyId, int level)
    {
        return Ok(await _members.GetByGenerationAsync(new InputGetMembersByGeneration
        {
            FamilyId = familyId,
            Level = level
        }));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OutputGetMember>> GetById(long id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        return Ok(member);
    }

    [HttpPost]
    public async Task<ActionResult<OutputGetMember>> Create([FromBody] InputCreateMemberView view)
    {
        var input = MapToCreateMemberInput(view);
        if (await _families.GetByIdAsync(input.FK_Families) is null)
        {
            return NotFound(new { message = "Family not found." });
        }

        var error = await ValidateMemberCreateAsync(input, null);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var created = await _members.CreateAsync(input, "system");

        if (input.FK_Members_Spouse.HasValue)
        {
            await _members.LinkSpouseAsync(created.ID_Members, input.FK_Members_Spouse.Value, "system");
            created = (await _members.GetByIdAsync(created.ID_Members))!;
        }

        return CreatedAtAction(nameof(GetById), new { id = created.ID_Members }, created);
    }

    [HttpPost("{memberId:long}/spouse")]
    public async Task<ActionResult<OutputGetMember>> AddSpouse(long memberId, [FromBody] InputAddSpouseView view)
    {
        var member = await _members.GetByIdAsync(memberId);
        if (member is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        OutputGetMember? spouse;

        if (view.ExistingSpouseId.HasValue)
        {
            spouse = await _members.GetByIdAsync(view.ExistingSpouseId.Value);
            if (spouse is null)
            {
                return NotFound(new { message = "Spouse member not found." });
            }

            if (spouse.FK_Families != member.FK_Families)
            {
                return Conflict(new { message = "Spouse must belong to the same family." });
            }
        }
        else if (view.NewSpouse is not null)
        {
            view.NewSpouse.FK_Families = member.FK_Families;
            view.NewSpouse.IsRoot = false;

            var createInput = MapToCreateMemberInput(view.NewSpouse);
            var error = await ValidateMemberCreateAsync(createInput, null);
            if (error is not null)
            {
                return BadRequest(new { message = error });
            }

            spouse = await _members.CreateAsync(createInput, "system");
        }
        else
        {
            return BadRequest(new { message = "ExistingSpouseId or NewSpouse is required." });
        }

        var selfError = _validation.ValidateSelfReference(memberId, null, spouse.ID_Members);
        if (selfError is not null)
        {
            return BadRequest(new { message = selfError });
        }

        await _members.LinkSpouseAsync(memberId, spouse.ID_Members, "system");
        var updated = await _members.GetByIdAsync(memberId);
        return Ok(updated);
    }

    [HttpPost("{parentId:long}/child")]
    public async Task<ActionResult<OutputGetMember>> AddChild(long parentId, [FromBody] InputAddChildView view)
    {
        var parent = await _members.GetByIdAsync(parentId);
        if (parent is null)
        {
            return NotFound(new { message = "Parent member not found." });
        }

        view.Child.FK_Families = parent.FK_Families;
        view.Child.FK_Members_Parent = parentId;
        view.Child.IsRoot = false;

        var input = MapToCreateMemberInput(view.Child);
        var error = await ValidateMemberCreateAsync(input, null);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var child = await _members.CreateAsync(input, "system");
        return CreatedAtAction(nameof(GetById), new { id = child.ID_Members }, child);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<OutputGetMember>> Update(long id, [FromBody] InputUpdateMemberView view)
    {
        var existing = await _members.GetByIdAsync(id);
        if (existing is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var input = MapToUpdateMemberInput(view);
        var error = await ValidateMemberUpdateAsync(id, existing.FK_Families, input);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var updated = await _members.UpdateAsync(id, input, "system");
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        if (!await _members.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Member not found." });
        }

        return NoContent();
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<List<OutputGetMemberAddress>>> GetAllAddresses()
    {
        return Ok(await _addresses.GetAllAsync());
    }

    [HttpGet("addresses/{id:long}")]
    public async Task<ActionResult<OutputGetMemberAddress>> GetAddressById(long id)
    {
        var item = await _addresses.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound(new { message = "Address not found." });
        }

        return Ok(item);
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<OutputGetMemberAddress>> CreateAddress([FromBody] InputCreateMemberAddressView view)
    {
        if (await _members.GetByIdAsync(view.FK_Members) is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var created = await _addresses.CreateAsync(MapToCreateAddressInput(view), "system");
        return CreatedAtAction(nameof(GetAddressById), new { id = created.ID_MemberAddresses }, created);
    }

    [HttpPut("addresses/{id:long}")]
    public async Task<ActionResult<OutputGetMemberAddress>> UpdateAddress(long id, [FromBody] InputUpdateMemberAddressView view)
    {
        var updated = await _addresses.UpdateAsync(id, MapToUpdateAddressInput(view), "system");
        if (updated is null)
        {
            return NotFound(new { message = "Address not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("addresses/{id:long}")]
    public async Task<IActionResult> DeleteAddress(long id)
    {
        if (!await _addresses.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Address not found." });
        }

        return NoContent();
    }

    [HttpGet("images")]
    public async Task<ActionResult<List<OutputGetMemberImage>>> GetAllImages()
    {
        return Ok(await _images.GetAllAsync());
    }

    [HttpGet("images/{id:long}")]
    public async Task<ActionResult<OutputGetMemberImage>> GetImageById(long id)
    {
        var item = await _images.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound(new { message = "Image not found." });
        }

        return Ok(item);
    }

    [HttpPost("images")]
    public async Task<ActionResult<OutputGetMemberImage>> CreateImage([FromBody] InputCreateMemberImageView view)
    {
        if (await _members.GetByIdAsync(view.FK_Members) is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var created = await _images.CreateAsync(MapToCreateImageInput(view), "system");
        return CreatedAtAction(nameof(GetImageById), new { id = created.ID_MemberImages }, created);
    }

    [HttpPut("images/{id:long}")]
    public async Task<ActionResult<OutputGetMemberImage>> UpdateImage(long id, [FromBody] InputUpdateMemberImageView view)
    {
        var updated = await _images.UpdateAsync(id, MapToUpdateImageInput(view), "system");
        if (updated is null)
        {
            return NotFound(new { message = "Image not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("images/{id:long}")]
    public async Task<IActionResult> DeleteImage(long id)
    {
        if (!await _images.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Image not found." });
        }

        return NoContent();
    }

    [HttpGet("events")]
    public async Task<ActionResult<List<OutputGetMemberEvent>>> GetAllEvents()
    {
        return Ok(await _events.GetAllAsync());
    }

    [HttpGet("events/{id:long}")]
    public async Task<ActionResult<OutputGetMemberEvent>> GetEventById(long id)
    {
        var item = await _events.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(item);
    }

    [HttpPost("events")]
    public async Task<ActionResult<OutputGetMemberEvent>> CreateEvent([FromBody] InputCreateMemberEventView view)
    {
        if (await _members.GetByIdAsync(view.FK_Members) is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var created = await _events.CreateAsync(MapToCreateEventInput(view), "system");
        return CreatedAtAction(nameof(GetEventById), new { id = created.ID_MemberEvents }, created);
    }

    [HttpPut("events/{id:long}")]
    public async Task<ActionResult<OutputGetMemberEvent>> UpdateEvent(long id, [FromBody] InputUpdateMemberEventView view)
    {
        var updated = await _events.UpdateAsync(id, MapToUpdateEventInput(view), "system");
        if (updated is null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("events/{id:long}")]
    public async Task<IActionResult> DeleteEvent(long id)
    {
        if (!await _events.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Event not found." });
        }

        return NoContent();
    }

    [HttpGet("social-links")]
    public async Task<ActionResult<List<OutputGetMemberSocialLink>>> GetAllSocialLinks()
    {
        return Ok(await _socialLinks.GetAllAsync());
    }

    [HttpGet("social-links/{id:long}")]
    public async Task<ActionResult<OutputGetMemberSocialLink>> GetSocialLinkById(long id)
    {
        var item = await _socialLinks.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound(new { message = "Social link not found." });
        }

        return Ok(item);
    }

    [HttpPost("social-links")]
    public async Task<ActionResult<OutputGetMemberSocialLink>> CreateSocialLink([FromBody] InputCreateMemberSocialLinkView view)
    {
        if (await _members.GetByIdAsync(view.FK_Members) is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var created = await _socialLinks.CreateAsync(MapToCreateSocialLinkInput(view), "system");
        return CreatedAtAction(nameof(GetSocialLinkById), new { id = created.ID_MemberSocialLinks }, created);
    }

    [HttpPut("social-links/{id:long}")]
    public async Task<ActionResult<OutputGetMemberSocialLink>> UpdateSocialLink(long id, [FromBody] InputUpdateMemberSocialLinkView view)
    {
        var updated = await _socialLinks.UpdateAsync(id, MapToUpdateSocialLinkInput(view), "system");
        if (updated is null)
        {
            return NotFound(new { message = "Social link not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("social-links/{id:long}")]
    public async Task<IActionResult> DeleteSocialLink(long id)
    {
        if (!await _socialLinks.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Social link not found." });
        }

        return NoContent();
    }

    [HttpGet("notes")]
    public async Task<ActionResult<List<OutputGetMemberNote>>> GetAllNotes()
    {
        return Ok(await _notes.GetAllAsync());
    }

    [HttpGet("notes/{id:long}")]
    public async Task<ActionResult<OutputGetMemberNote>> GetNoteById(long id)
    {
        var item = await _notes.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound(new { message = "Note not found." });
        }

        return Ok(item);
    }

    [HttpPost("notes")]
    public async Task<ActionResult<OutputGetMemberNote>> CreateNote([FromBody] InputCreateMemberNoteView view)
    {
        if (await _members.GetByIdAsync(view.FK_Members) is null)
        {
            return NotFound(new { message = "Member not found." });
        }

        var created = await _notes.CreateAsync(MapToCreateNoteInput(view), "system");
        return CreatedAtAction(nameof(GetNoteById), new { id = created.ID_MemberNotes }, created);
    }

    [HttpPut("notes/{id:long}")]
    public async Task<ActionResult<OutputGetMemberNote>> UpdateNote(long id, [FromBody] InputUpdateMemberNoteView view)
    {
        var updated = await _notes.UpdateAsync(id, MapToUpdateNoteInput(view), "system");
        if (updated is null)
        {
            return NotFound(new { message = "Note not found." });
        }

        return Ok(updated);
    }

    [HttpDelete("notes/{id:long}")]
    public async Task<IActionResult> DeleteNote(long id)
    {
        if (!await _notes.SoftDeleteAsync(id, "system"))
        {
            return NotFound(new { message = "Note not found." });
        }

        return NoContent();
    }

    private async Task<string?> ValidateMemberCreateAsync(InputCreateMember input, long? memberId)
    {
        var emailError = _validation.ValidateEmail(input.Email);
        if (emailError is not null) return emailError;

        var phoneError = _validation.ValidatePhone(input.Phone);
        if (phoneError is not null) return phoneError;

        var selfError = _validation.ValidateSelfReference(memberId ?? 0, input.FK_Members_Parent, input.FK_Members_Spouse);
        if (selfError is not null) return selfError;

        if (input.IsRoot)
        {
            var rootError = _validation.ValidateRootMember(true, await _members.HasRootMemberAsync(input.FK_Families, memberId));
            if (rootError is not null) return rootError;
        }

        if (input.FK_Members_Parent.HasValue)
        {
            return await _validation.ValidateCircularParentAsync(memberId ?? 0, input.FK_Members_Parent, _members.GetParentIdAsync);
        }

        return null;
    }

    private async Task<string?> ValidateMemberUpdateAsync(long memberId, long familyId, InputUpdateMember input)
    {
        var emailError = _validation.ValidateEmail(input.Email);
        if (emailError is not null) return emailError;

        var phoneError = _validation.ValidatePhone(input.Phone);
        if (phoneError is not null) return phoneError;

        var selfError = _validation.ValidateSelfReference(memberId, input.FK_Members_Parent, input.FK_Members_Spouse);
        if (selfError is not null) return selfError;

        if (input.IsRoot)
        {
            var rootError = _validation.ValidateRootMember(true, await _members.HasRootMemberAsync(familyId, memberId));
            if (rootError is not null) return rootError;
        }

        if (input.FK_Members_Parent.HasValue)
        {
            return await _validation.ValidateCircularParentAsync(memberId, input.FK_Members_Parent, _members.GetParentIdAsync);
        }

        return null;
    }

    private static InputCreateMember MapToCreateMemberInput(InputCreateMemberView view) => new()
    {
        FK_Families = view.FK_Families,
        FK_Members_Parent = view.FK_Members_Parent,
        FK_Members_Spouse = view.FK_Members_Spouse,
        FirstName = view.FirstName,
        LastName = view.LastName,
        Email = view.Email,
        Phone = view.Phone,
        Gender = view.Gender,
        DateOfBirth = view.DateOfBirth,
        DateOfDeath = view.DateOfDeath,
        IsRoot = view.IsRoot,
        Biography = view.Biography,
        Profession = view.Profession
    };

    private static InputUpdateMember MapToUpdateMemberInput(InputUpdateMemberView view) => new()
    {
        FK_Members_Parent = view.FK_Members_Parent,
        FK_Members_Spouse = view.FK_Members_Spouse,
        FirstName = view.FirstName,
        LastName = view.LastName,
        Email = view.Email,
        Phone = view.Phone,
        Gender = view.Gender,
        DateOfBirth = view.DateOfBirth,
        DateOfDeath = view.DateOfDeath,
        IsRoot = view.IsRoot,
        Biography = view.Biography,
        Profession = view.Profession
    };

    private static InputCreateMemberAddress MapToCreateAddressInput(InputCreateMemberAddressView view) => new()
    {
        FK_Members = view.FK_Members,
        AddressLine1 = view.AddressLine1,
        AddressLine2 = view.AddressLine2,
        City = view.City,
        State = view.State,
        Country = view.Country,
        PostalCode = view.PostalCode,
        IsPrimary = view.IsPrimary
    };

    private static InputUpdateMemberAddress MapToUpdateAddressInput(InputUpdateMemberAddressView view) => new()
    {
        AddressLine1 = view.AddressLine1,
        AddressLine2 = view.AddressLine2,
        City = view.City,
        State = view.State,
        Country = view.Country,
        PostalCode = view.PostalCode,
        IsPrimary = view.IsPrimary
    };

    private static InputCreateMemberImage MapToCreateImageInput(InputCreateMemberImageView view) => new()
    {
        FK_Members = view.FK_Members,
        ImageUrl = view.ImageUrl,
        Caption = view.Caption,
        IsPrimary = view.IsPrimary,
        SortOrder = view.SortOrder
    };

    private static InputUpdateMemberImage MapToUpdateImageInput(InputUpdateMemberImageView view) => new()
    {
        ImageUrl = view.ImageUrl,
        Caption = view.Caption,
        IsPrimary = view.IsPrimary,
        SortOrder = view.SortOrder
    };

    private static InputCreateMemberEvent MapToCreateEventInput(InputCreateMemberEventView view) => new()
    {
        FK_Members = view.FK_Members,
        EventType = view.EventType,
        Title = view.Title,
        Description = view.Description,
        EventDate = view.EventDate
    };

    private static InputUpdateMemberEvent MapToUpdateEventInput(InputUpdateMemberEventView view) => new()
    {
        EventType = view.EventType,
        Title = view.Title,
        Description = view.Description,
        EventDate = view.EventDate
    };

    private static InputCreateMemberSocialLink MapToCreateSocialLinkInput(InputCreateMemberSocialLinkView view) => new()
    {
        FK_Members = view.FK_Members,
        Platform = view.Platform,
        Url = view.Url,
        Username = view.Username
    };

    private static InputUpdateMemberSocialLink MapToUpdateSocialLinkInput(InputUpdateMemberSocialLinkView view) => new()
    {
        Platform = view.Platform,
        Url = view.Url,
        Username = view.Username
    };

    private static InputCreateMemberNote MapToCreateNoteInput(InputCreateMemberNoteView view) => new()
    {
        FK_Members = view.FK_Members,
        Title = view.Title,
        Content = view.Content
    };

    private static InputUpdateMemberNote MapToUpdateNoteInput(InputUpdateMemberNoteView view) => new()
    {
        Title = view.Title,
        Content = view.Content
    };
}
