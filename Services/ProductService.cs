using auth.Models;
using auth.Repositories.Interfaces;
using auth.Services.Interfaces;
using AutoMapper;
using auth.Models;
using auth.Repositories.Interfaces;
using auth.Services.Interfaces;

namespace auth.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<ServiceResult<IEnumerable<ProductViewModel>>> GetAllProductsAsync()
        {
            try
            {
                var products = await _productRepository.GetAllAsync();
                var productVms = _mapper.Map<IEnumerable<ProductViewModel>>(products);
                return ServiceResult<IEnumerable<ProductViewModel>>.SuccessResult(productVms);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<ProductViewModel>>.FailureResult($"Erreur: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ProductViewModel>> GetProductByIdAsync(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                    return ServiceResult<ProductViewModel>.FailureResult("Produit non trouvé");

                var productVm = _mapper.Map<ProductViewModel>(product);
                return ServiceResult<ProductViewModel>.SuccessResult(productVm);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductViewModel>.FailureResult($"Erreur: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ProductViewModel>> CreateProductAsync(ProductViewModel productVm)
        {
            try
            {
                var product = _mapper.Map<Product>(productVm);
                product.CreatedAt = DateTime.UtcNow;
                product.IsActive = true;

                var createdProduct = await _productRepository.CreateAsync(product);
                var resultVm = _mapper.Map<ProductViewModel>(createdProduct);

                return ServiceResult<ProductViewModel>.SuccessResult(resultVm, "Produit créé avec succès");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductViewModel>.FailureResult($"Erreur: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ProductViewModel>> UpdateProductAsync(int id, ProductViewModel productVm)
        {
            try
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                    return ServiceResult<ProductViewModel>.FailureResult("Produit non trouvé");

                _mapper.Map(productVm, existingProduct);
                existingProduct.UpdatedAt = DateTime.UtcNow;

                var updatedProduct = await _productRepository.UpdateAsync(existingProduct);
                var resultVm = _mapper.Map<ProductViewModel>(updatedProduct);

                return ServiceResult<ProductViewModel>.SuccessResult(resultVm, "Produit modifié avec succès");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductViewModel>.FailureResult($"Erreur: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteProductAsync(int id)
        {
            try
            {
                var result = await _productRepository.DeleteAsync(id);
                if (!result)
                    return ServiceResult<bool>.FailureResult("Produit non trouvé");

                return ServiceResult<bool>.SuccessResult(true, "Produit supprimé avec succès");
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.FailureResult($"Erreur: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ProductViewModel>>> SearchProductsAsync(string searchTerm)
        {
            try
            {
                var products = await _productRepository.SearchAsync(searchTerm);
                var productVms = _mapper.Map<IEnumerable<ProductViewModel>>(products);
                return ServiceResult<IEnumerable<ProductViewModel>>.SuccessResult(productVms);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<ProductViewModel>>.FailureResult($"Erreur: {ex.Message}");
            }
        }

        public async Task<ServiceResult<int>> GetProductsCountAsync()
        {
            try
            {
                var count = await _productRepository.GetCountAsync();
                return ServiceResult<int>.SuccessResult(count);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.FailureResult($"Erreur: {ex.Message}");
            }
        }
    }
}