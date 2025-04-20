using Microsoft.EntityFrameworkCore.Storage.Json;

namespace Coil.Api.Contracts
{
    public class ProductRequest
    {
        public string Name { get; set; }
        public string Specification { get; set; } 
        public decimal Price { get; set; }
        public List<IFormFile> Files { get; set; }
    }
}
