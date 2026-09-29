namespace MshMsh.Domain
{
    public class OrderItem
    {
        public int Id { get; private set; }
        public int OrderId { get; private set; }
        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public int Quantity { get; private set; }
        public decimal UnitCostPrice { get; private set; }
        public decimal UnitSellingPrice { get; private set; }

        public decimal TotalPrice => Quantity * UnitSellingPrice;
        public decimal TotalCost => Quantity * UnitCostPrice;
        public decimal Profit => TotalPrice - TotalCost;

        private OrderItem() { }

        public OrderItem(Product product, int quantity, decimal? customSellingPrice = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product), "يجب تحديد المنتج.");

            if (quantity <= 0)
                throw new ArgumentException("الكمية يجب أن تكون أكبر من الصفر.");

            ProductId = product.Id;
            Product = product;
            Quantity = quantity;
            UnitCostPrice = product.CostPrice;
            UnitSellingPrice = customSellingPrice.HasValue && customSellingPrice.Value > 0
                ? customSellingPrice.Value
                : product.SellingPrice;
        }
    }
}