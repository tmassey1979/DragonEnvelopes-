namespace DragonEnvelopes.Desktop.Services;

public interface IFamilyContext
{
    Guid? FamilyId { get; }

    void SetFamilyId(Guid? familyId);
}
