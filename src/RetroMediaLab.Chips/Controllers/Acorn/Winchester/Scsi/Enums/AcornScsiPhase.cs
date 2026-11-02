namespace RetroMediaLab.Chips.Controllers.Acorn.Winchester.Scsi.Enums;

/// <summary>Étapes internes du contrôleur AcornScsi.</summary>
public enum AcornScsiPhase { Free, Selection, Command, Executing, Read, Write, Formatting, Status, Message }
