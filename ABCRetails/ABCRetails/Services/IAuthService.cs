using ABCRetails.Models;
using ABCRetails.Models.ViewModels; // ADD THIS USING DIRECTIVE

namespace ABCRetails.Services
{
    public interface IAuthService
    {
        Task<Customer?> AuthenticateAsync(string email, string password);
        Task<bool> RegisterAsync(RegisterViewModel model);
        Task<bool> CustomerExistsAsync(string email);
        Task<Customer?> GetCustomerByIdAsync(string id);
        Task<Customer?> GetCustomerByEmailAsync(string email);
    }
}