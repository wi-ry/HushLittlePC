# Capturing WinUI crash dumps

Check `%LocalAppData%\HushLittlePC\winui-app.log` first. The WinUI application logs each startup step and managed unhandled exceptions there; for example, a missing XAML resource shows up as a `XamlParseException` in `MainWindow.InitializeComponent`.

A process termination with exception code `0xC000027B` can be a fail-fast stowed exception, in which case those managed handlers may not receive a useful exception before the process terminates.

Use Windows Error Reporting LocalDumps to capture the underlying exception data.

## Enable dump collection

Open **PowerShell as Administrator** from the repository root and run:

```powershell
.\scripts\Enable-WinUICrashDumps.ps1
```

Run Hush Little PC, reproduce the crash, and look for a `.dmp` file under:

```text
%LOCALAPPDATA%\HushLittlePC\CrashDumps
```

The script configures full dumps for `HushLittlePC.WinUI.exe` only and retains up to five dumps.

## Inspect the stowed exception

Install or open WinDbg, load the dump, and run:

```text
.load pde
!pde.dse
```

For a `0xC000027B` crash, `!pde.dse` decodes the stowed exception records, including their HRESULTs and captured originating call stacks. The first stowed exception is often the most useful; if it is a rethrow, inspect the following records too.

The faulting module reported by Event Viewer is often only where the deferred exception becomes fatal. The decoded stowed exception is needed to identify the original failure.

## Disable dump collection

After reproducing the problem, disable the per-application setting from an elevated PowerShell session:

```powershell
.\scripts\Enable-WinUICrashDumps.ps1 -Disable
```

## Privacy

Full process dumps can contain private data present in application memory. Keep the dump local and do not attach it to a public issue or pull request. Share the text output from `!pde.dse` after reviewing it for sensitive information instead.
