using Microsoft.EntityFrameworkCore;
using MshMsh.Data;
using MshMsh.Domain;

namespace MshMsh.Services
{
    public class OrderService
    {
        // إنشاء فاتورة بيع جديدة مع دعم السعر المعدل من الكاشير والخصم
        public async Task<Order> CreateOrderAsync(int customerId, decimal discount, List<(int ProductId, int Quantity, decimal UnitPrice)> itemsToBuy)
        {
            if (itemsToBuy == null || itemsToBuy.Count == 0)
                throw new ArgumentException("لا يمكن إنشاء فاتورة بدون أصناف.");

            using var context = new AppDbContext();

            var customer = await context.Customers.FindAsync(customerId);
            if (customer == null) throw new KeyNotFoundException("العميل غير موجود.");

            var order = new Order(customerId, discount);

            foreach (var item in itemsToBuy)
            {
                var product = await context.Products.FindAsync(item.ProductId);
                if (product == null) throw new KeyNotFoundException($"المنتج برقم {item.ProductId} غير موجود.");

                // تمرير سعر البيع الفعلي المحسوب في الشاشة
                order.AddItem(product, item.Quantity, item.UnitPrice);
            }

            if (discount > 0)
            {
                order.ApplyDiscount(discount);
            }

            await context.Orders.AddAsync(order);
            await context.SaveChangesAsync();

            return order;
        }

        // جلب الفاتورة مع تفاصيل بنودها ومنتجاتها لشاشة المرتجع
        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            using var context = new AppDbContext();
            return await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        // تسجيل استرجاع صنف محدد وإعادة الكمية للمخزن
        public async Task ProcessItemReturnAsync(int orderId, int productId, int returnQty)
        {
            using var context = new AppDbContext();

            var order = await context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new KeyNotFoundException("رقم الفاتورة غير صحيح.");

            var orderItem = order.Items.FirstOrDefault(i => i.ProductId == productId);
            if (orderItem == null) throw new InvalidOperationException("هذا المنتج غير موجود بالفاتورة الأصلية.");

            // حساب ما تم استرجاعه سابقاً من هذا الصنف لنفس الفاتورة
            var previousReturns = await context.ReturnTransactions
                .Where(r => r.OrderId == orderId && r.ProductId == productId)
                .SumAsync(r => r.ReturnedQuantity);

            int allowedToReturn = orderItem.Quantity - previousReturns;
            if (returnQty > allowedToReturn)
                throw new InvalidOperationException($"الكمية المسموح بإرجاعها حالياً هي {allowedToReturn} فقط.");

            var product = await context.Products.FindAsync(productId);
            if (product == null) throw new KeyNotFoundException("المنتج غير موجود بالمخزن.");

            // إنشاء حركة استرجاع وحفظها وإعادة المخزون
            var returnTx = new ReturnTransaction(orderId, product, returnQty, orderItem.UnitSellingPrice);
            await context.ReturnTransactions.AddAsync(returnTx);
            await context.SaveChangesAsync();
        }

        // جلب سجل كل المرتجعات للتقارير
        public async Task<List<ReturnTransaction>> GetAllReturnsAsync()
        {
            using var context = new AppDbContext();
            return await context.ReturnTransactions
                .Include(r => r.Product)
                .Include(r => r.Order)
                .ThenInclude(o => o.Customer)
                .AsNoTracking()
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();
        }

        // جلب كل الفواتير للعرض في التقارير وشاشة المرتجعات
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            using var context = new AppDbContext();
            return await context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}