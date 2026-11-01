global using RetroMediaLab.Emulation.Sony.Common.Exceptions;
global using RetroMediaLab.Emulation.Sony.Common.Enums;
global using RetroMediaLab.Emulation.Sony.Common.Functions;
global using EmulatorManagementContext = RetroMediaLab.Emulation.Contracts.EmulatorManagementContext;
global using CoreOptionValue = RetroMediaLab.Emulation.Contracts.EmulationOptionValue;
global using PendingCommand = RetroMediaLab.Emulation.Contracts.EmulationMachineCommand;
global using RetroMediaLab.Emulation.Constants;
global using RetroMediaLab.Emulation.Contracts;
global using RetroMediaLab.Emulation.Dictionaries;
global using RetroMediaLab.Emulation.Enums;
global using RetroMediaLab.Emulation.Exceptions;
global using RetroMediaLab.Emulation.Functions;
global using RetroMediaLab.Emulation.Interfaces;
global using RetroMediaLab.Emulation.Interop;
global using RetroMediaLab.Emulation.Services;
global using RetroMediaLab.Emulation.Sony.Common.Constants;
global using RetroMediaLab.Emulation.Sony.Common.Dictionaries;
global using RetroMediaLab.Emulation.Sony.Common.Interfaces;
global using RetroMediaLab.Emulation.Sony.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Sony.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Sony.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Sony.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Sony.Common.Machines.Common.Exceptions;
global using RetroMediaLab.Emulation.Sony.Common.Machines.Common.Functions;
global using RetroMediaLab.Emulation.Sony.Common.Services;

global using InputAccumulator = RetroMediaLab.Emulation.Interfaces.IEmulationInputAccumulator;
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Sony.Common.Machines.Common.Enums.Emulator>;
global using CoreReleaseSettings = RetroMediaLab.Emulation.Contracts.EmulationCoreReleaseSettings;
global using EmulatorCatalogEntry = RetroMediaLab.Emulation.Contracts.EmulationDefinedCatalogEntry<RetroMediaLab.Emulation.Sony.Common.Machines.Common.Enums.Emulator>;

global using SavedStateHeader = RetroMediaLab.Emulation.Contracts.EmulationSavedStateHeader<string, string>;

global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Sony.Common.Machines.Common.Contracts.MachineConfiguration>;
global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;

global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Enums.EmulationMessageCategory, RetroMediaLab.Emulation.Enums.EmulationMessageCode>;

global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
