using FormsBackend.DTOs;

namespace FormsBackend.Services
{
    public interface IFormService
    {
        Task<FormResponseDto> CreateFormAsync(CreateFormDto dto, string createdBy);
        Task<FormResponseDto> GetFormByIdAsync(string id);
        Task<IEnumerable<FormResponseDto>> GetAllFormsAsync();
        Task<FormResponseDto> PublishFormAsync(string id, string publishedBy);
        Task<FormResponseDto> UpdateFormAsync(string id, UpdateFormDto dto, string updatedBy);
        Task<bool> DeleteFormAsync(string id);
    }
}