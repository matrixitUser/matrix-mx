using System.Data.Entity;
using Matrix.Domain.Entities;

namespace Matrix.Domain.Infrastructure.EntityFramework
{
    class ContextStrategy: DropCreateDatabaseAlways<Context>
    {
        protected override void Seed(Context context)
        {
            //context.Areas.Add(new Area {Street = "ASDASDF"});
        }
    }
}
