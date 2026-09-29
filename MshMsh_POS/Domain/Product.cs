namespace MshMsh.Domain
{
    public class Product
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = null!;
        public decimal CostPrice { get; private set; }    // سعر الشراء
        public decimal SellingPrice { get; private set; } // سعر البيع
        public int StockQuantity { get; private set; }

        private Product() { }

        public Product(string name, decimal costPrice, decimal sellingPrice, int stockQuantity)
        {
            Validate(name, costPrice, sellingPrice, stockQuantity);

            Name = name.Trim();
            CostPrice = costPrice;
            SellingPrice = sellingPrice;
            StockQuantity = stockQuantity;
        }

        public void Update(string name, decimal costPrice, decimal sellingPrice, int stockQuantity)
        {
            Validate(name, costPrice, sellingPrice, stockQuantity);

            Name = name.Trim();
            CostPrice = costPrice;
            SellingPrice = sellingPrice;
            StockQuantity = stockQuantity;
        }

        public void ReduceStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("الكمية المطلوبة يجب أن تكون أكبر من الصفر.");

            if (quantity > StockQuantity)
                throw new InvalidOperationException($"الكمية في المخزن غير كافية! المتوفر حالياً: {StockQuantity}");

            StockQuantity -= quantity;
        }

        public void IncreaseStock(int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentException("الكمية المضافة يجب أن تكون أكبر من الصفر.");

            StockQuantity += quantity;
        }

        private void Validate(string name, decimal costPrice, decimal sellingPrice, int stockQuantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم المنتج لا يمكن أن يكون فارغاً.");

            if (costPrice < 0)
                throw new ArgumentException("سعر الشراء لا يمكن أن يكون سالباً.");

            if (sellingPrice <= 0)
                throw new ArgumentException("سعر البيع يجب أن يكون أكبر من الصفر.");

            if (stockQuantity < 0)
                throw new ArgumentException("كمية المخزن لا يمكن أن تكون سالبة.");
        }
    }
}