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
global using RetroMediaLab.Emulation.Bandai.Common.Constants;
global using RetroMediaLab.Emulation.Bandai.Common.Dictionaries;
global using RetroMediaLab.Emulation.Bandai.Common.Enums;
global using RetroMediaLab.Emulation.Bandai.Common.Exceptions;
global using RetroMediaLab.Emulation.Bandai.Common.Functions;
global using RetroMediaLab.Emulation.Bandai.Common.Interfaces;
global using RetroMediaLab.Emulation.Bandai.Common.Services;
global using RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Functions;


global using LoadedContent = RetroMediaLab.Emulation.Interfaces.IEmulationLoadedContent;

global using ExternalCoreExports = RetroMediaLab.Emulation.Contracts.ExternalCoreExports;

global using CoreActiveInstallation = RetroMediaLab.Emulation.Contracts.EmulationCoreActiveInstallation;
global using CoreDiagnosticManifest = RetroMediaLab.Emulation.Contracts.EmulationCoreDiagnosticManifest;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;

global using CoreOptionCategory = RetroMediaLab.Emulation.Contracts.EmulationOptionCategory;
global using CoreOptionValue = RetroMediaLab.Emulation.Contracts.EmulationOptionValue;

global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.Emulator>;
global using EmulatorCatalogEntry = RetroMediaLab.Emulation.Contracts.EmulationCoreCatalogEntry<RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.MachineModel>;
global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Bandai.Common.Enums.ErrorCategory, RetroMediaLab.Emulation.Bandai.Common.Enums.ErrorCode>;

global using SavedStateHeader = RetroMediaLab.Emulation.Contracts.EmulationSavedStateHeader<RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.Emulator>;
global using StateContentEntry = RetroMediaLab.Emulation.Contracts.EmulationStateContentEntry;
global using StateControllerFingerprint = RetroMediaLab.Emulation.Contracts.EmulationStateControllerFingerprint<RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.PeripheralCategory>;
global using StateFile = RetroMediaLab.Emulation.Contracts.EmulationSavedStateFile<RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.Emulator>;

global using ConfigurationDocument = RetroMediaLab.Emulation.Contracts.EmulationConfigurationDocument<RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Contracts.FirmwareConfiguration, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Contracts.MediaConfiguration, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Contracts.InputConfiguration, RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Contracts.FolderConfiguration>;

global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Contracts.MachineConfiguration>;
