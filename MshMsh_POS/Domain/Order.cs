namespace MshMsh.Domain
{
    public class Order
    {
        public int Id { get; private set; }
        public DateTime OrderDate { get; private set; }

        public int CustomerId { get; private set; }
        public Customer Customer { get; private set; } = null!;

        public decimal Discount { get; private set; }

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        public decimal SubTotal => _items.Sum(item => item.TotalPrice);
        public decimal TotalAmount => Math.Max(0, SubTotal - Discount);
        public decimal TotalCost => _items.Sum(item => item.TotalCost);
        public decimal TotalProfit => Math.Max(0, (SubTotal - TotalCost) - Discount);

        private Order() { }

        public Order(int customerId, decimal discount = 0)
        {
            if (customerId <= 0)
                throw new ArgumentException("يجب تحديد عميل صالح.");

            if (discount < 0)
                throw new ArgumentException("قيمة الخصم لا يمكن أن تكون سالبة.");

            CustomerId = customerId;
            Discount = discount;
            OrderDate = DateTime.Now;
        }

        // دعم إدخال سعر بيع مخصص إذا قام الأدمن بتعديله
        public void AddItem(Product product, int quantity, decimal? customSellingPrice = null)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            decimal unitPriceToUse = customSellingPrice.HasValue && customSellingPrice.Value > 0
                ? customSellingPrice.Value
                : product.SellingPrice;

            var existingItem = _items.FirstOrDefault(i => i.ProductId == product.Id && i.UnitSellingPrice == unitPriceToUse);
            if (existingItem != null)
            {
                product.ReduceStock(quantity);
                _items.Remove(existingItem);
                _items.Add(new OrderItem(product, existingItem.Quantity + quantity, unitPriceToUse));
            }
            else
            {
                product.ReduceStock(quantity);
                _items.Add(new OrderItem(product, quantity, unitPriceToUse));
            }
        }

        public void ApplyDiscount(decimal discount)
        {
            if (discount < 0 || discount > SubTotal)
                throw new ArgumentException("قيمة الخصم غير صالحة.");

            Discount = discount;
        }
    }
}