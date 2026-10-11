using Content.Shared._Floof.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Localizations;

namespace Content.Shared.IoC
{
    public static class SharedContentIoC
    {
        public static void Register(IDependencyCollection deps)
        {
            deps.Register<MarkingManager, MarkingManager>();
            deps.Register<ContentLocalizationManager, ContentLocalizationManager>();

            // Begin Euphoria additions
            deps.Register<IHumanoidProfileMigrationsManager, HumanoidProfileMigrationsManager>();
            // End Euphoria additions
        }
    }
}
