using App.Core.Entities;
using App.DAL.Presistence;
using App.DAL.Repositories.Interfaces;

namespace App.DAL.Repositories.Abstractions
{
    public class DivisionRepository : Repository<Division>, IDivisionRepository
    {
        public DivisionRepository(AppDbContext context) : base(context) { }
    }
}
