using MidnightApi.Data;
using MidnightApi.DataAccess.Interfaces;
using MidnightApi.Models;
using MidnightApi.Repositories.Interfaces;

namespace MidnightApi.Repositories;

public class MemoriesRepository : IMemoriesRepository
{
    private readonly IDataAccessDapper _dataAccessDapper;

    public MemoriesRepository(IDataAccessDapper dataAccessDapper)
    {
        _dataAccessDapper = dataAccessDapper;
    }

    public async Task<OutputPagedMemories?> GetListAsync(InputMemoryList input)
    {
        var (items, totalCount) = await _dataAccessDapper.GetPagedListByStoredProcedureAsync<OutputMemoryListItem>(
            StoredProcedures.MemoryList, input);

        var page = Math.Max(input.Page, 1);
        var pageSize = Math.Clamp(input.PageSize <= 0 ? 12 : input.PageSize, 1, 50);

        return new OutputPagedMemories
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public Task<OutputGetMemory?> GetByIdAsync(InputGetMemory input) =>
        _dataAccessDapper.GetPayloadByStoredProcedureAsync<OutputGetMemory>(
            StoredProcedures.MemorySelect, input);

    public Task<OutputCreateMemory> CreateAsync(InputCreateMemory input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputCreateMemory>(
            StoredProcedures.MemoryInsert, input);

    public Task<OutputUpdateMemory> UpdateAsync(InputUpdateMemory input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputUpdateMemory>(
            StoredProcedures.MemoryUpdate, input);

    public Task<OutputDeleteMemory> SoftDeleteAsync(InputDeleteMemory input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputDeleteMemory>(
            StoredProcedures.MemoryDelete, input);

    public Task<OutputMemoryImageUploadContext?> GetImageUploadContextAsync(InputMemoryImageUploadContext input) =>
        _dataAccessDapper.GetPayloadByStoredProcedureAsync<OutputMemoryImageUploadContext>(
            StoredProcedures.MemoryImageUploadContext, input);

    public Task<OutputMemoryImageCommit> CommitImageAsync(InputMemoryImageCommit input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputMemoryImageCommit>(
            StoredProcedures.MemoryImageCommit, input);

    public Task<OutputMemoryImageDetail?> GetImageAsync(InputMemoryImageGet input) =>
        _dataAccessDapper.GetPayloadByStoredProcedureAsync<OutputMemoryImageDetail>(
            StoredProcedures.MemoryImageGet, input);

    public Task<OutputMemoryImageDelete> SoftDeleteImageAsync(InputMemoryImageDelete input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputMemoryImageDelete>(
            StoredProcedures.MemoryImageDelete, input);

    public Task<OutputMemoryCoverSet> SetCoverAsync(InputMemoryCoverSet input) =>
        _dataAccessDapper.GetSingleByStoredProcedureAsync<OutputMemoryCoverSet>(
            StoredProcedures.MemoryCoverSet, input);
}
