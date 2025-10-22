using FormsBackend.Models.Mongo;

namespace FormsBackend.Repositories
{
    public interface IFormRepository
    {
        Task<Form> CreateFormAsync(Form form);
        Task<Form> GetFormByIdAsync(string id);
        Task<IEnumerable<Form>> GetAllFormsAsync();
        Task<Form> PublishFormAsync(string id, string publishedBy);
        Task<Form> UpdateFormAsync(Form form);
        Task<bool> DeleteFormAsync(string id);
    }
}