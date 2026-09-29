namespace MshMsh.Domain
{
    public class ReturnTransaction
    {
        public int Id { get; private set; }
        public int OrderId { get; private set; }
        public Order Order { get; private set; } = null!;

        public int ProductId { get; private set; }
        public Product Product { get; private set; } = null!;

        public int ReturnedQuantity { get; private set; }
        public decimal RefundAmount { get; private set; }
        public DateTime ReturnDate { get; private set; }

        private ReturnTransaction() { }

        public ReturnTransaction(int orderId, Product product, int quantity, decimal unitRefundPrice)
        {
            if (orderId <= 0) throw new ArgumentException("رقم الفاتورة غير صحيح.");
            if (product == null) throw new ArgumentNullException(nameof(product));
            if (quantity <= 0) throw new ArgumentException("الكمية المرتجعة يجب أن تكون أكبر من الصفر.");

            OrderId = orderId;
            ProductId = product.Id;
            Product = product;
            ReturnedQuantity = quantity;
            RefundAmount = quantity * unitRefundPrice;
            ReturnDate = DateTime.Now;

            // إعادة البضاعة للمخزن فورياً
            product.IncreaseStock(quantity);
        }
    }
}