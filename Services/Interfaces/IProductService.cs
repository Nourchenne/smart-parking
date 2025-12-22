using auth.Models;
using auth.Models;

namespace auth.Services.Interfaces
{
    public interface IProductService
    {
        Task<ServiceResult<IEnumerable<ProductViewModel>>> GetAllProductsAsync();
        Task<ServiceResult<ProductViewModel>> GetProductByIdAsync(int id);
        Task<ServiceResult<ProductViewModel>> CreateProductAsync(ProductViewModel productVm);
        Task<ServiceResult<ProductViewModel>> UpdateProductAsync(int id, ProductViewModel productVm);
        Task<ServiceResult<bool>> DeleteProductAsync(int id);
        Task<ServiceResult<IEnumerable<ProductViewModel>>> SearchProductsAsync(string searchTerm);
        Task<ServiceResult<int>> GetProductsCountAsync();
    }

    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ServiceResult<T> SuccessResult(T data, string message = "")
        {
            return new ServiceResult<T> { Success = true, Data = data, Message = message };
        }

        public static ServiceResult<T> FailureResult(string message)
        {
            return new ServiceResult<T> { Success = false, Message = message };
        }

        public static ServiceResult<T> ValidationFailed(List<string> errors)
        {
            return new ServiceResult<T> { Success = false, Message = "Validation failed", Errors = errors };
        }
    }
}