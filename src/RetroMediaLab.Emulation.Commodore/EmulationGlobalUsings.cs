global using RetroMediaLab.Emulation.Commodore.Common.Exceptions;
global using RetroMediaLab.Emulation.Commodore.Common.Enums;
global using RetroMediaLab.Emulation.Commodore.Common.Functions;
global using SavedStateHeader = RetroMediaLab.Emulation.Contracts.EmulationSavedStateHeader<string, string>;
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
global using RetroMediaLab.Emulation.Commodore.Common.Constants;
global using RetroMediaLab.Emulation.Commodore.Common.Dictionaries;
global using RetroMediaLab.Emulation.Commodore.Common.Interfaces;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Constants;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Contracts;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Dictionaries;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Enums;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Exceptions;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Functions;
global using RetroMediaLab.Emulation.Commodore.Modules;
global using RetroMediaLab.Emulation.Commodore.Common.Services;
global using RetroMediaLab.Emulation.Commodore.Emulators.Common.Interop.Contracts;
global using RetroMediaLab.Emulation.Commodore.Emulators.Common.Interop.Enums;

global using RetroMediaLab.Emulation.Commodore.Emulators.Common.Interop.Constants;
global using RetroMediaLab.Emulation.Commodore.Emulators.Common.Interop.Services;
global using RetroMediaLab.Emulation.Commodore.Emulators.Common.Interop.Factories;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Contracts;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Constants;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Functions;
global using AmigaFirmwareCatalog = RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Dictionaries.AmigaFirmwareCatalog;
global using AmigaSettingsHelpDictionary = RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Dictionaries.AmigaSettingsHelpDictionary;
global using RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Enums;
global using RetroMediaLab.Emulation.Commodore.Emulators.UAE.Common.Interop.Factories;
global using RetroMediaLab.Emulation.Commodore.Emulators.UAE.Common.Interop.Functions;
global using RetroMediaLab.Emulation.Commodore.Emulators.UAE.Common.Interop.Constants;

global using InputAccumulator = RetroMediaLab.Emulation.Interfaces.IEmulationInputAccumulator;

global using CoreOption = RetroMediaLab.Emulation.Contracts.EmulationOption;
global using CoreRelease = RetroMediaLab.Emulation.Contracts.EmulationCatalogCoreRelease<RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Enums.Emulator>;
global using EmulatorCatalogEntry = RetroMediaLab.Emulation.Contracts.EmulationDefinedCatalogEntry<RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Enums.Emulator>;

global using EmulatorCreationContext = RetroMediaLab.Emulation.Contracts.EmulatorCreationContext<RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Contracts.MachineConfiguration>;

global using HostError = RetroMediaLab.Emulation.Contracts.HostError<RetroMediaLab.Emulation.Enums.EmulationMessageCategory, RetroMediaLab.Emulation.Enums.EmulationMessageCode>;

global using CoreInstallProgress = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallProgress;
global using CoreInstallationPaths = RetroMediaLab.Emulation.Contracts.EmulationCoreInstallationPaths;
