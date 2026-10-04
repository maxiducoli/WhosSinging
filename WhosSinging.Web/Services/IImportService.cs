namespace WhosSinging.Web.Services;

public interface IImportService
{
    Task<int> ImportAllAsync();
}