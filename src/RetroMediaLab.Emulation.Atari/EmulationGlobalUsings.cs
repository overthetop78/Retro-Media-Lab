global using RetroMediaLab.Emulation.Atari.Common.Exceptions;
global using DiskImageStatus = RetroMediaLab.Emulation.Contracts.DiskImageStatus;
global using DiskStatus = RetroMediaLab.Emulation.Contracts.DiskStatus;
global using EmulatorManagementContext = RetroMediaLab.Emulation.Contracts.EmulatorManagementContext;
global using CoreOptionValue = RetroMediaLab.Emulation.Contracts.EmulationOptionValue;
global using AudioBuffer = RetroMediaLab.Emulation.Interfaces.IEmulationAudioBuffer;
global using MachineCommand = RetroMediaLab.Emulation.Contracts.EmulationMachineCommand;
global using RetroMediaLab.Emulation.Constants;
global using RetroMediaLab.Emulation.Contracts;
global using RetroMediaLab.Emulation.Dictionaries;
global using RetroMediaLab.Emulation.Enums;
global using RetroMediaLab.Emulation.Exceptions;
global using RetroMediaLab.Emulation.Functions;
global using RetroMediaLab.Emulation.Interfaces;
global using RetroMediaLab.Emulation.Interop;
global using RetroMediaLab.Emulation.Services;
global using RetroMediaLab.Emulation.Atari.Common.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Interfaces;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Contracts;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Enums;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Functions;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari2600.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari2600.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari5200.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari5200.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari7800.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.Atari7800.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariJaguar.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariJaguar.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariLynx.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariLynx.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariST.Constants;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariST.Contracts;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariST.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariST.Enums;
global using RetroMediaLab.Emulation.Atari.Common.Machines.AtariST.Functions;
global using RetroMediaLab.Emulation.Atari.Modules;
global using RetroMediaLab.Emulation.Atari.Common.Services;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Constants;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Contracts;
global using RetroMediaLab.Emulation.Atari.Common.Enums;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Exceptions;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Functions;

global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Constants;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Contracts;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Dictionaries;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Factories;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Functions;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Interfaces;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Services;
global using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Exceptions;


global using LoadedContent = RetroMediaLab.Emulation.Interfaces.IEmulationLoadedContent;

global using ExternalCoreExports = RetroMediaLab.Emulation.Contracts.ExternalCoreExports;

global using CoreActiveInstallation = RetroMediaLab.Emulation.Contracts.EmulationCoreActiveInstallation;
global using CoreDiagnosticManifest = RetroMediaLab.Emulation.Contracts.EmulationCoreDiagnosticManifest;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;

global using CoreOptionCategory = RetroMediaLab.Emulation.Contracts.EmulationOptionCategory;

global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.Emulator>;
global using EmulatorCatalogEntry = RetroMediaLab.Emulation.Contracts.EmulationCoreCatalogEntry<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.MachineModel>;
global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Atari.Common.Enums.ErrorCategory, RetroMediaLab.Emulation.Atari.Common.Enums.ErrorCode>;

global using SavedStateHeader = RetroMediaLab.Emulation.Contracts.EmulationSavedStateHeader<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.Emulator>;
global using StateContentEntry = RetroMediaLab.Emulation.Contracts.EmulationStateContentEntry;
global using StateControllerFingerprint = RetroMediaLab.Emulation.Contracts.EmulationStateControllerFingerprint<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.PeripheralCategory>;
global using StateFile = RetroMediaLab.Emulation.Contracts.EmulationSavedStateFile<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.Emulator>;

global using ConfigurationDocument = RetroMediaLab.Emulation.Contracts.EmulationConfigurationDocument<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.FirmwareConfiguration, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.MediaConfiguration, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.InputConfiguration, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.FolderConfiguration>;

global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.MachineConfiguration>;

// Les observations partagées conservent la définition typée du catalogue du module.
global using ScannedFirmware = RetroMediaLab.Emulation.Contracts.EmulationFirmwareObservation<RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.FirmwareDefinition>;
global using FirmwareCompatibility = RetroMediaLab.Emulation.Enums.EmulationFirmwareCompatibility;
global using FirmwareDetectionStatus = RetroMediaLab.Emulation.Enums.EmulationFirmwareDetectionStatus;
