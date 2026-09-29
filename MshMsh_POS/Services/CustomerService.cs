using Microsoft.EntityFrameworkCore;
using MshMsh.Data;
using MshMsh.Domain;

namespace MshMsh.Services
{
    public class CustomerService
    {
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            using var context = new AppDbContext();
            return await context.Customers.AsNoTracking().ToListAsync();
        }

        public async Task<Customer> GetOrCreateCustomerAsync(string name, string phone)
        {
            using var context = new AppDbContext();
            var customer = await context.Customers.FirstOrDefaultAsync(c => c.Phone == phone.Trim());

            if (customer == null)
            {
                customer = new Customer(name, phone);
                await context.Customers.AddAsync(customer);
                await context.SaveChangesAsync();
            }

            return customer;
        }

        public async Task AddCustomerAsync(string name, string phone)
        {
            using var context = new AppDbContext();
            var customer = new Customer(name, phone);
            await context.Customers.AddAsync(customer);
            await context.SaveChangesAsync();
        }

        public async Task UpdateCustomerAsync(int id, string name, string phone)
        {
            using var context = new AppDbContext();
            var customer = await context.Customers.FindAsync(id);
            if (customer == null) throw new KeyNotFoundException("العميل غير موجود.");

            customer.Update(name, phone);
            await context.SaveChangesAsync();
        }

        // جلب كشف حساب ومشتريات العميل
        public async Task<(decimal TotalSpent, int OrdersCount, List<Order> Orders)> GetCustomerPurchasesAsync(int customerId)
        {
            using var context = new AppDbContext();
            var orders = await context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .AsNoTracking()
                .ToListAsync();

            decimal totalSpent = orders.Sum(o => o.TotalAmount);
            return (totalSpent, orders.Count, orders);
        }
    }
}