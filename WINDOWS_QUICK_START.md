# BradleyGRBL Windows Quick Start

## Install

1. Download the Windows ZIP from the latest GitHub release.
2. Right-click the ZIP and select **Extract All**.
3. Open the extracted folder.
4. Double-click `START_BradleyGRBL.bat`.
5. If Windows SmartScreen appears, select **More info**, verify the source is
   `XXKingBradXX/BradleyGRBL`, then select **Run anyway**.

The package is portable and does not need an installer. Windows 10 or 11 with
.NET Framework 4.x is recommended.

## Test The Outline

1. Import an image and finish the normal engraving conversion.
2. Select **Trace Preview** to verify the outline without machine movement.
3. Connect the engraver and wait for the status to show **Idle**.
4. For the first physical test, remove the workpiece and keep the beam path
   clear.
5. Select **Run Outline at 3%**.
6. Check the displayed PWM and speed, wear laser safety glasses, then confirm.

The machine action uses 3% of the controller's configured maximum PWM. For
example, `$30=1000` produces `S30`.

## Safety

- Never leave the engraver unattended.
- Use enclosure/interlocks and wavelength-appropriate safety glasses.
- Confirm the machine origin and available travel before tracing.
- Press **Stop** immediately if the path is unexpected.
- The generated outline is an envelope of laser-on engraving moves. Complex,
  disconnected artwork can produce a path that bridges separate shapes.

## Source And License

BradleyGRBL is a modified version of LaserGRBL and remains licensed under
GPLv3. The complete corresponding source is in the GitHub repository.
