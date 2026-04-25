using ApiApp.Models;

namespace ApiApp.BudgetService
{
    public class BudgetService
    {
        private readonly AppDbContext _db;
        public BudgetService(AppDbContext db)
        {
            _db = db;
        }

        //public Budget Insert(string dto)
        //{
        //    Budget response;
        //    return response;
        //}
    }
}
