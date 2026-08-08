namespace MidnightApi.DataAccess;

public interface IDataAccessDapper
{
    Task<List<TResult>> GetListByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null);
    Task<(List<TResult> Items, int TotalCount)> GetPagedListByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null);
    Task<TResult?> GetSingleOrDefaultByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null);
    Task<TResult> GetSingleByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null);
    Task<TResult?> GetPayloadByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null);
    Task<TResult?> ExecuteScalarByStoredProcedureAsync<TResult>(string storedProcedureName, object? parameter = null);
    Task<int> ExecuteByStoredProcedureAsync(string storedProcedureName, object? parameter = null);
}
