namespace PatchNotes.Data;

public interface IHasCreatedAt
{
    DateTimeOffset CreatedAt { get; set; }
}

public interface IHasUpdatedAt
{
    DateTimeOffset UpdatedAt { get; set; }
}
