global using DiskImageStatus = RetroMediaLab.Emulation.Contracts.DiskImageStatus;
global using DiskStatus = RetroMediaLab.Emulation.Contracts.DiskStatus;
global using EmulatorManagementContext = RetroMediaLab.Emulation.Contracts.EmulatorManagementContext;
global using EnvironmentCallbackHost = RetroMediaLab.Emulation.Interfaces.IEmulationEnvironmentCallbackHost;
global using CoreOptionValue = RetroMediaLab.Emulation.Contracts.EmulationOptionValue;
global using VideoCallbackHost = RetroMediaLab.Emulation.Interfaces.IEmulationVideoCallbackHost;
global using AudioCallbackHost = RetroMediaLab.Emulation.Interfaces.IEmulationAudioCallbackHost;
global using MachineCommand = RetroMediaLab.Emulation.Contracts.EmulationMachineCommand;
global using RetroMediaLab.Emulation;
global using RetroMediaLab.Emulation.Constants;
global using RetroMediaLab.Emulation.Contracts;
global using RetroMediaLab.Emulation.Dictionaries;
global using RetroMediaLab.Emulation.Enums;
global using RetroMediaLab.Emulation.Exceptions;
global using RetroMediaLab.Emulation.Functions;
global using RetroMediaLab.Emulation.Interfaces;
global using RetroMediaLab.Emulation.Services;
global using RetroMediaLab.Emulation.Thomson.Common.Constants;
global using RetroMediaLab.Emulation.Thomson.Common.Contracts;
global using RetroMediaLab.Emulation.Thomson.Common.Interfaces;
global using RetroMediaLab.Emulation.Thomson.Common.Services;
global using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Functions;
global using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Thomson.Common.Exceptions;
global using RetroMediaLab.Emulation.Thomson.Common.Enums;
global using RetroMediaLab.Emulation.Thomson.Common.Functions;


global using ExternalCoreExports = RetroMediaLab.Emulation.Contracts.ExternalCoreExports;
global using CoreActiveInstallation = RetroMediaLab.Emulation.Contracts.EmulationCoreActiveInstallation;
global using CoreDiagnosticManifest = RetroMediaLab.Emulation.Contracts.EmulationCoreDiagnosticManifest;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;

global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.Emulator>;
global using EmulatorCatalogEntry = RetroMediaLab.Emulation.Contracts.EmulationCoreCatalogEntry<RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.MachineModel>;
global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Enums.EmulationMessageCategory, RetroMediaLab.Emulation.Enums.EmulationMessageCode>;

global using SavedStateHeader = RetroMediaLab.Emulation.Contracts.EmulationSavedStateHeader<RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.Emulator>;
global using StateContentEntry = RetroMediaLab.Emulation.Contracts.EmulationStateContentEntry;
global using StateControllerFingerprint = RetroMediaLab.Emulation.Contracts.EmulationStateControllerFingerprint<RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.PeripheralCategory>;
global using StateFile = RetroMediaLab.Emulation.Contracts.EmulationSavedStateFile<RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums.Emulator>;

global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Contracts.MachineConfiguration>;
