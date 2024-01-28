# Bitwarden-Backup

A simple c# console app that integrates with the Bitwarden CLI (Command Line Interface) to export your [`Bitwarden Vault`](https://bitwarden.com/help/cli/), utilizing [`Serilog`](https://github.com/serilog/serilog) for logging and [`Spectre.Console`](https://github.com/spectreconsole/spectre.console) for console prompts.

## Requirements
- Bitwarden CLI Executable
- .NET 8.0 SDK

## Installation
1. Clone `Bitwarden-Backup` repository and build it, or download the corresponding release file.
1. Make a copy of the `appsettings-example.json` file and name it `appsettings.json`.
    - The appsettings can be left as is and will run prompting you for all the needed values.
1. Download and install `.NET 8.0 SDK` (See [`Bitwarden CLI Download`](https://bitwarden.com/help/cli/)).
1. Download `Bitwarden CLI Executable` (See [`.NET 8.0 SDK Download`](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)).
1. Place the `Bitwarden CLI Executable` in the same directory as the `Bitwarden-Backup` executable 

## Configurable appsetting.json Options
These options can be configured by setting the values for the keys in the appsettings.json file.

| Key  | Default | Example | Description |
| ---- | ---- | ---- | ---- |
| `MinimumLevel:Default` | `Debug` | `Information` | The minimum log event level written to the log file. See [`Serilog Minimum Level`](https://github.com/serilog/serilog/wiki/Configuration-Basics#minimum-level). |
| `WriteTo:Args:path` | - | `Logs/log.txt` | The file name or path to the file name. If the directories to the file names don't exist, it will be created. |
| `WriteTo:Args:rollingInterval` | `Infinite` | `Day` | The frequency at which the log file should roll. See [`Serilog Rolling Interval`](https://github.com/serilog/serilog-sinks-file/blob/dev/src/Serilog.Sinks.File/RollingInterval.cs). |
| `WriteTo:Args:retainedFileCountLimit` | `31` | `null` | The number of files to retain. |
| `WriteTo:Args:shared` | `false` | `true` | By default, only one process may write to a log file at a given time. Setting this allows multi-process shared log files. |
| `WriteTo:Args:outputTemplate` | `{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}` | `{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u5}] {Message:lj}{NewLine}{Exception}` | The format for each log entry. See [`Serilog Formatting Output`](https://github.com/serilog/serilog/wiki/Formatting-Output). |
| `ExportFile:Name` | `bw_export` | `C:\Temp\bw_export` | The path to place the exported file with the file name. |
| `ExportFile:DateInFileNameFormat` | - | `yyyyMMdd` | When set, a date string based on the format is appended to the exported file name. See [`Format Specifier`](https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings). |
| `ExportFile:Format` | `json` | `encrypted_json` | The file format of the exported file. See [`Export Format`](https://github.com/stchao/Bitwarden-Backup/blob/main/Bitwarden-Backup/Models/Enums.cs) for all options. |
| `ExportFile:CustomExportPassword` | - | `custompw` | When `ExportFile:Format` is set to `encrypted_json` and this is set, the file will be encrypted with this password instead of the Bitwarden's account encryption key. |
| `BitwardenConfiguration:Url` | - | `https://your.bw.domain.com` | The Bitwarden server to connect to. |
| `BitwardenConfiguration:UserLogInMethod` | `None` | `EmailPw` | The method to log in to your Bitwarden vault. See [`Log In Method`](https://github.com/stchao/Bitwarden-Backup/blob/main/Bitwarden-Backup/Models/Enums.cs) for all options. |
| `BitwardenConfiguration:EnableInteractiveLogIn` | `true` | `false` | If you want to be prompted for any missing but required values from the appsettings.json file. |
| `BitwardenConfiguration:ExecutablePath` | `Executing Directory` | `C:\Temp` | The path to the bw.exe file. |
| `Credentials:EmailPasswordCredential:Email` | - | `email@example.com` | The email address for your Bitwarden vault. |
| `Credentials:EmailPasswordCredential:MasterPassword` or `Credentials:APIKeyCredential:MasterPassword` | - | `bwpassword` | The master password for your Bitwarden vault. |
| `Credentials:EmailPasswordCredential:UserTwoFactorMethod` | `None` | `Email` | The two factor method to unlock your Bitwarden vault. See [`Two Factor Method`](https://github.com/stchao/Bitwarden-Backup/blob/main/Bitwarden-Backup/Models/Enums.cs) for all options. |
| `Credentials:EmailPasswordCredential:TwoFactorCode` | - | `999999` | The two factor code corresponding to the two factor method. |
| `Credentials:APIKeyCredential:ClientId` | - | `user.clientId` | A value unique to your account. See [`Personal API Key`](https://bitwarden.com/help/personal-api-key/) for how to obtain it. |
| `Credentials:APIKeyCredential:ClientSecret` | - | `clientSecret` | A unique value that can be rotated. See [`Personal API Key`](https://bitwarden.com/help/personal-api-key/) for how to obtain it. |