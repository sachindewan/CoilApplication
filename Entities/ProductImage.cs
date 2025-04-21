using System.ComponentModel.DataAnnotations.Schema;

namespace Coil.Api.Entities
{
    public class ProductImage
    {
        public Guid Id { get; set; }
        public string Uri { get; set; }
        public Guid ProductId { get; set; }
    }
}
