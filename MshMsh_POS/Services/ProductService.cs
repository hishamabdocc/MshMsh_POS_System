using Microsoft.EntityFrameworkCore;
using MshMsh.Data;
using MshMsh.Domain;

namespace MshMsh.Services
{
    public class ProductService
    {
        public async Task<List<Product>> GetAllProductsAsync()
        {
            using var context = new AppDbContext();
            return await context.Products.AsNoTracking().ToListAsync();
        }

        public async Task AddProductAsync(string name, decimal costPrice, decimal sellingPrice, int stockQuantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم المنتج لا يمكن أن يكون فارغاً.");

            using var context = new AppDbContext();

            // فحص منع تكرار اسم المنتج
            bool exists = await context.Products.AnyAsync(p => p.Name.ToLower() == name.Trim().ToLower());
            if (exists)
                throw new InvalidOperationException($"المنتج '{name.Trim()}' مسجل مسبقاً في المخزن!");

            var product = new Product(name, costPrice, sellingPrice, stockQuantity);
            await context.Products.AddAsync(product);
            await context.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(int id, string name, decimal costPrice, decimal sellingPrice, int stockQuantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم المنتج لا يمكن أن يكون فارغاً.");

            using var context = new AppDbContext();

            // فحص منع التكرار مع استثناء نفس السجل الحالي
            bool exists = await context.Products.AnyAsync(p => p.Id != id && p.Name.ToLower() == name.Trim().ToLower());
            if (exists)
                throw new InvalidOperationException($"يوجد منتج آخر مسجل بالفعل باسم '{name.Trim()}'!");

            var product = await context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException("المنتج غير موجود.");

            product.Update(name, costPrice, sellingPrice, stockQuantity);
            await context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            using var context = new AppDbContext();
            var product = await context.Products.FindAsync(id);

            if (product == null)
                throw new KeyNotFoundException("المنتج غير موجود.");

            context.Products.Remove(product);
            await context.SaveChangesAsync();
        }
    }
}