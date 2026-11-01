global using DiskImageStatus = RetroMediaLab.Emulation.Contracts.DiskImageStatus;
global using DiskStatus = RetroMediaLab.Emulation.Contracts.DiskStatus;
global using EmulatorManagementContext = RetroMediaLab.Emulation.Contracts.EmulatorManagementContext;
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
global using RetroMediaLab.Emulation;
global using RetroMediaLab.Emulation.Snk.Common.Constants;
global using RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Snk.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Snk.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Snk.Common.Machines.Common.Functions;
global using RetroMediaLab.Emulation.Snk.Common.Interfaces;
global using RetroMediaLab.Emulation.Snk.Common.Services;
global using RetroMediaLab.Emulation.Snk.Common.Enums;
global using RetroMediaLab.Emulation.Snk.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Snk.Common.Dictionaries;
global using RetroMediaLab.Emulation.Snk.Common.Exceptions;
global using RetroMediaLab.Emulation.Snk.Common.Functions;



global using ExternalCoreExports = RetroMediaLab.Emulation.Contracts.ExternalCoreExports;

global using CoreActiveInstallation = RetroMediaLab.Emulation.Contracts.EmulationCoreActiveInstallation;
global using CoreDiagnosticManifest = RetroMediaLab.Emulation.Contracts.EmulationCoreDiagnosticManifest;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;

global using CoreOptionCategory = RetroMediaLab.Emulation.Contracts.EmulationOptionCategory;
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.Emulator>;
global using EmulatorCatalogEntry = RetroMediaLab.Emulation.Contracts.EmulationCoreCatalogEntry<RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.MachineModel>;
global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Snk.Common.Enums.ErrorCategory, RetroMediaLab.Emulation.Snk.Common.Enums.ErrorCode>;


global using SavedStateHeader = RetroMediaLab.Emulation.Contracts.EmulationSavedStateHeader<RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.Emulator>;
global using StateContentEntry = RetroMediaLab.Emulation.Contracts.EmulationStateContentEntry;
global using StateControllerFingerprint = RetroMediaLab.Emulation.Contracts.EmulationStateControllerFingerprint<RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.PeripheralCategory>;
global using StateFile = RetroMediaLab.Emulation.Contracts.EmulationSavedStateFile<RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.Emulator>;

global using ConfigurationDocument = RetroMediaLab.Emulation.Contracts.EmulationConfigurationContentDocument<RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Contracts.FirmwareConfiguration, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Contracts.MediaConfiguration, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Contracts.InputConfiguration, RetroMediaLab.Emulation.Snk.Common.Machines.Common.Contracts.FolderConfiguration>;

global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Snk.Common.Machines.Common.Contracts.MachineConfiguration>;
global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;
global using CoreOptionValue = RetroMediaLab.Emulation.Contracts.EmulationOptionValue;
global using FrameSkipRatio = RetroMediaLab.Emulation.Enums.EmulationFrameSkipRatio;
