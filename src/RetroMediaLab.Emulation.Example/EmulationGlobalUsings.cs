// EmulationGlobalUsings fixe les contrats et interfaces disponibles au module.
// Aucun import de RetroMediaLab.Emulation.Functions : les composants passent par le Common du mock.
global using RetroMediaLab.Emulation.Contracts;
global using RetroMediaLab.Emulation.Enums;
global using RetroMediaLab.Emulation.Interfaces;

// Les types de données, descriptions et règles appartiennent au Common du module.
global using RetroMediaLab.Emulation;
global using RetroMediaLab.Emulation.Example.Common.Constants;
global using RetroMediaLab.Emulation.Example.Common.Contracts;
global using RetroMediaLab.Emulation.Example.Common.Dictionaries;
global using RetroMediaLab.Emulation.Example.Common.Enums;
global using RetroMediaLab.Emulation.Example.Common.Exceptions;
global using RetroMediaLab.Emulation.Example.Common.Interfaces;
global using RetroMediaLab.Emulation.Example.Common.Services;
global using RetroMediaLab.Emulation.Example.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Example.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Example.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Example.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Example.Common.Machines.Common.Functions;

// Ces alias nomment des contrats publics existants ; ils ne créent ni classe ni appel supplémentaire.
global using ConfigurationDocument = RetroMediaLab.Emulation.Contracts.EmulationConfigurationSlotsDocument<RetroMediaLab.Emulation.Example.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Example.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Example.Common.Machines.Common.Contracts.FirmwareConfiguration, RetroMediaLab.Emulation.Example.Common.Machines.Common.Contracts.MediaConfiguration, RetroMediaLab.Emulation.Example.Common.Machines.Common.Contracts.InputConfiguration, RetroMediaLab.Emulation.Example.Common.Machines.Common.Contracts.FolderConfiguration>;
global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Example.Common.Machines.Common.Contracts.MachineConfiguration>;
global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Example.Common.Enums.ErrorCategory, RetroMediaLab.Emulation.Example.Common.Enums.ErrorCode>;

// Les observations partagées conservent la définition typée du catalogue du module.
global using ScannedFirmware = RetroMediaLab.Emulation.Contracts.EmulationFirmwareObservation<RetroMediaLab.Emulation.Example.Common.Machines.Common.Contracts.FirmwareDefinition>;
global using FirmwareCompatibility = RetroMediaLab.Emulation.Enums.EmulationFirmwareCompatibility;
global using FirmwareDetectionStatus = RetroMediaLab.Emulation.Enums.EmulationFirmwareDetectionStatus;
global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;
global using CoreOptionValue = RetroMediaLab.Emulation.Contracts.EmulationOptionValue;
global using FrameSkipRatio = RetroMediaLab.Emulation.Enums.EmulationFrameSkipRatio;

// Le service de versions et le fournisseur utilisent les mêmes données SDK que les catalogues des autres modules.
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Example.Common.Machines.Common.Enums.Emulator>;
global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
