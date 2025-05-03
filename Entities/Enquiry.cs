using System.Numerics;

namespace Coil.Api.Entities
{
    public class Enquiry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string RowMatrial { get; set; } = string.Empty;
        public double? Quantity { get; set; }

        public Int64 MobileNumber { get; set; }

    }
}
