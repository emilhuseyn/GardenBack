using App.Core.Exceptions.Commons;
using App.Core.Entities.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using App.DAL.Handlers.Interfaces;

namespace App.DAL.Handlers.Implementations
{
    public class Handler<T> : IHandler<T> where T : BaseEntity
    {
        public T HandleEntityAsync(T entity)
        {
            if (entity is null)
                throw new EntityNotFoundException($"Entity of type {typeof(T).Name} not found in the database.");

            return entity;
        }
    }
}
