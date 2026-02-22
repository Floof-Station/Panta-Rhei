using Content.Shared._EE.CCVars;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;

namespace Content.Server.Atmos.EntitySystems
{
    public sealed partial class AtmosphereSystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public bool SpaceWind { get; private set; }
        public float SpaceWindPressureForceDivisorThrow { get; private set; }
        public float SpaceWindPressureForceDivisorPush { get; private set; }
        public float SpaceWindMaxVelocity { get; private set; }
        public float SpaceWindMaxPushForce { get; private set; }
        public bool MonstermosEqualization { get; private set; }
        public bool MonstermosDepressurization { get; private set; }
        public bool MonstermosRipTiles { get; private set; }
        public bool GridImpulse { get; private set; }
        public float SpacingEscapeRatio { get; private set; }
        public float SpacingMinGas { get; private set; }
        public float SpacingMaxWind { get; private set; }
        public bool Superconduction { get; private set; }
        public bool ExcitedGroups { get; private set; }
        public bool ExcitedGroupsSpaceIsAllConsuming { get; private set; }
        public float AtmosMaxProcessTime { get; private set; }
        public float AtmosTickRate { get; private set; }
        public float Speedup { get; private set; }
        public float HeatScale { get; private set; }
        public bool DeltaPressureDamage { get; private set; }
        public int DeltaPressureParallelProcessPerIteration { get; private set; }
        public int DeltaPressureParallelBatchSize { get; private set; }

        // EE section - space wind
        public float SpaceWindMinimumCalculatedMass { get; private set; }
        public float SpaceWindMaximumCalculatedInverseMass { get; private set; }
        public bool MonstermosUseExpensiveAirflow { get; private set; }
        public float MonstermosRipTilesMinimumPressure { get; private set; }
        public float MonstermosRipTilesPressureOffset { get; private set; }
        public float HumanoidThrowMultiplier { get; private set; }
        // EE section end

        /// <summary>
        /// Time between each atmos sub-update.  If you are writing an atmos device, use AtmosDeviceUpdateEvent.dt
        /// instead of this value, because atmos devices do not update each are sub-update and sometimes are skipped to
        /// meet the tick deadline.
        /// </summary>
        public float AtmosTime => 1f / AtmosTickRate;

        private void InitializeCVars()
        {
            Subs.CVar(_cfg, CCVars.SpaceWind, value => SpaceWind = value, true);
            Subs.CVar(_cfg, CCVars.SpaceWindPressureForceDivisorThrow, value => SpaceWindPressureForceDivisorThrow = value, true);
            Subs.CVar(_cfg, CCVars.SpaceWindPressureForceDivisorPush, value => SpaceWindPressureForceDivisorPush = value, true);
            Subs.CVar(_cfg, CCVars.SpaceWindMaxVelocity, value => SpaceWindMaxVelocity = value, true);
            Subs.CVar(_cfg, CCVars.SpaceWindMaxPushForce, value => SpaceWindMaxPushForce = value, true);
            Subs.CVar(_cfg, CCVars.MonstermosEqualization, value => MonstermosEqualization = value, true);
            Subs.CVar(_cfg, CCVars.MonstermosDepressurization, value => MonstermosDepressurization = value, true);
            Subs.CVar(_cfg, CCVars.MonstermosRipTiles, value => MonstermosRipTiles = value, true);
            Subs.CVar(_cfg, CCVars.AtmosGridImpulse, value => GridImpulse = value, true);
            Subs.CVar(_cfg, CCVars.AtmosSpacingEscapeRatio, value => SpacingEscapeRatio = value, true);
            Subs.CVar(_cfg, CCVars.AtmosSpacingMinGas, value => SpacingMinGas = value, true);
            Subs.CVar(_cfg, CCVars.AtmosSpacingMaxWind, value => SpacingMaxWind = value, true);
            Subs.CVar(_cfg, CCVars.Superconduction, value => Superconduction = value, true);
            Subs.CVar(_cfg, CCVars.AtmosMaxProcessTime, value => AtmosMaxProcessTime = value, true);
            Subs.CVar(_cfg, CCVars.AtmosTickRate, value => AtmosTickRate = value, true);
            Subs.CVar(_cfg, CCVars.AtmosSpeedup, value => Speedup = value, true);
            Subs.CVar(_cfg, CCVars.AtmosHeatScale, value => { HeatScale = value; InitializeGases(); }, true);
            Subs.CVar(_cfg, CCVars.ExcitedGroups, value => ExcitedGroups = value, true);
            Subs.CVar(_cfg, CCVars.ExcitedGroupsSpaceIsAllConsuming, value => ExcitedGroupsSpaceIsAllConsuming = value, true);
            Subs.CVar(_cfg, CCVars.DeltaPressureDamage, value => DeltaPressureDamage = value, true);
            Subs.CVar(_cfg, CCVars.DeltaPressureParallelToProcessPerIteration, value => DeltaPressureParallelProcessPerIteration = value, true);
            Subs.CVar(_cfg, CCVars.DeltaPressureParallelBatchSize, value => DeltaPressureParallelBatchSize = value, true);

            // EE section
            // Note that SOME of the above CVars are now unused. Idk which ones. Fuck me.
            Subs.CVar(_cfg, EECVars.SpaceWindMinimumCalculatedMass, value => SpaceWindMinimumCalculatedMass = value, true);
            Subs.CVar(_cfg, EECVars.SpaceWindMaximumCalculatedInverseMass, value => SpaceWindMaximumCalculatedInverseMass = value, true);
            Subs.CVar(_cfg, EECVars.MonstermosUseExpensiveAirflow, value => MonstermosUseExpensiveAirflow = value, true);
            Subs.CVar(_cfg, EECVars.MonstermosRipTilesMinimumPressure, value => MonstermosRipTilesMinimumPressure = value, true);
            Subs.CVar(_cfg, EECVars.MonstermosRipTilesPressureOffset, value => MonstermosRipTilesPressureOffset = value, true);
            // Note sure if this is used either.
            Subs.CVar(_cfg, EECVars.AtmosHumanoidThrowMultiplier, value => HumanoidThrowMultiplier = value, true);
            // EE section end
        }
    }
}
