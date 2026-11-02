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

global using RetroMediaLab.Emulation.Acorn.Common.Constants;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Functions;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMaster.Constants;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMaster.Dictionaries;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Dictionaries;
global using RetroMediaLab.Emulation;
global using RetroMediaLab.Emulation.Acorn.Common.Services;
global using RetroMediaLab.Emulation.Acorn.Common.Enums;
global using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Acorn.Common.Dictionaries;
global using RetroMediaLab.Emulation.Acorn.Common.Interfaces;
global using RetroMediaLab.Emulation.Acorn.Common.Exceptions;
global using RetroMediaLab.Emulation.Acorn.Common.Functions;


global using LoadedContent = RetroMediaLab.Emulation.Interfaces.IEmulationLoadedContent;

global using ExternalCoreExports = RetroMediaLab.Emulation.Contracts.ExternalCoreExports;

global using CoreActiveInstallation = RetroMediaLab.Emulation.Contracts.EmulationCoreActiveInstallation;
global using CoreDiagnosticManifest = RetroMediaLab.Emulation.Contracts.EmulationCoreDiagnosticManifest;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;

global using CoreOptionCategory = RetroMediaLab.Emulation.Contracts.EmulationOptionCategory;
global using CoreOptionValue = RetroMediaLab.Emulation.Contracts.EmulationOptionValue;

global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums.Emulator>;
global using EmulatorCatalogEntry = RetroMediaLab.Emulation.Contracts.EmulationCoreCatalogEntry<RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums.MachineModel>;
global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Acorn.Common.Enums.ErrorCategory, RetroMediaLab.Emulation.Acorn.Common.Enums.ErrorCode>;


global using ConfigurationDocument = RetroMediaLab.Emulation.Contracts.EmulationConfigurationDocument<RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums.MachineModel, RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums.Emulator, RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts.FirmwareConfiguration, RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts.MediaConfiguration, RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts.InputConfiguration, RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts.FolderConfiguration>;

global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts.MachineConfiguration>;
