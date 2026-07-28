using System;

namespace Domain.Common;

public interface IBaseEntity
{
    Guid Id { get; set; }
}

public abstract class BaseEntity : IBaseEntity
{
    public Guid Id { get; set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
    }
}
