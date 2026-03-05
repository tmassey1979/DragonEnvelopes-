using CommunityToolkit.Mvvm.ComponentModel;

namespace DragonEnvelopes.Desktop.Services;

public sealed partial class FamilyContext : ObservableObject, IFamilyContext
{
    [ObservableProperty]
    private Guid? familyId;

    public void SetFamilyId(Guid? familyId)
    {
        FamilyId = familyId;
    }
}
