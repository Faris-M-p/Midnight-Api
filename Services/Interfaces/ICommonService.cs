using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MidnightApi.Models;

namespace MidnightApi.Services.Interfaces;

public interface ICommonService
{
    void ValidateModelState(ModelStateDictionary modelState);
    string? ToJson(object? value);
    void EnsureSuccess<T>(CommonResponse<T> result);
    IActionResult ToActionResult<T>(CommonResponse<T> result, string? traceId = null, int? httpStatus = null);
}
