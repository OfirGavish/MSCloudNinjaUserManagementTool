# MS Cloud Ninja User Offboarding Tool

A powerful and user-friendly Windows desktop application designed to streamline the process of offboarding users from Microsoft 365 environments. Built with .NET 8.0 and Microsoft Graph API, this tool provides a comprehensive solution for IT administrators to manage user offboarding and onboarding tasks efficiently.

## Features

### User Management
- **User Search and Selection**
  - Search and filter users from your Microsoft 365 environment
  - Select multiple users for batch processing
  - Modern dark-themed UI for comfortable usage

### Offboarding Features
- **Comprehensive Offboarding Actions**
  - Disable user accounts
  - Remove users from Global Address List (GAL)
  - Remove users from all groups
  - Remove user licenses
  - Update manager for reporting employees
  - Revoke user sign-in sessions
  - Detect on-premises synced users
  - Display required Active Directory actions

### Onboarding Features
- **License Management**
  - View available licenses with friendly names
  - Display license availability (used/total)
  - Automatic disable of unavailable licenses
  - Detailed tooltips with license information
  - Smart search for licenses by name or SKU
  - Color-coded license display (white for available, gray for unavailable)

- **User Creation**
  - Create new users with comprehensive details
  - Assign available licenses with usage location support
  - Set manager and groups (supports all group types)
  - Generate secure temporary passwords (auto-copied to clipboard)
  - Additional email configuration options
  - Support for mail-enabled security groups
  - Automatic error handling with detailed feedback

### Security & Compliance
- **Enhanced Security**
  - Secure authentication using Microsoft Graph API
  - Detailed logging of all operations
  - Error handling and operation status tracking
  - Progress tracking for batch operations
  - On-premises sync status detection

## Prerequisites

- Windows operating system
- .NET 8.0 Runtime
- Microsoft 365 administrator account with appropriate permissions
- Azure AD application registration with necessary Microsoft Graph API permissions

## Installation

To get started with the MS Cloud Ninja User Offboarding Tool:

1. Download our digitally signed installer from our secure storage:
   https://storage.mscloudninja.com/MSCNUserOffBoardingTool_Installer.exe
2. Execute the installer and follow the guided installation process
3. Launch the application from your Start Menu or Desktop shortcut

Note: The tool requires .NET 8.0 Runtime and a Windows operating system. Administrator privileges are recommended for installation.

## Usage

### Authentication
- Launch the application
- Click on the authentication button to sign in with your Microsoft 365 administrator account
- Grant the necessary permissions when prompted

### User Offboarding
1. **User Selection**
   - Use the search box to find specific users
   - Select one or multiple users from the grid
   - Users can be sorted by clicking on column headers

2. **Action Selection**
   - Choose the desired offboarding actions using the checkboxes
   - System automatically detects if users are synced from on-premises
   - Displays appropriate messages for required Active Directory actions

3. **Execution**
   - Click the "Execute" button to start the offboarding process
   - Monitor progress through the progress bar
   - View status updates in real-time
   - Check the logs for detailed operation information

### User Onboarding
1. **User Details**
   - Fill in the required user information
   - System validates input in real-time
   - Set usage location for proper license assignment

2. **License Assignment**
   - View available licenses with friendly names
   - See license availability (e.g., "45 available of 100 total")
   - Color-coded display for license availability
   - Tooltips show detailed license information
   - Unavailable licenses are automatically disabled
   - Search licenses by name or SKU

3. **Group Assignment**
   - Search and select groups (all types supported)
   - Assign manager
   - Set additional email properties
   - Configure usage location

4. **Password Management**
   - Secure temporary password generation
   - Automatic clipboard copy for easy sharing
   - Force password change on first login
   - Clear success messages with credential information

## Logging

The application maintains detailed logs of all operations for auditing and troubleshooting purposes. Log files are stored in:
```
%LocalAppData%\MSCloudNinja\Logs\app_YYYYMMDD.log
```
Where:
- `%LocalAppData%` is your Windows local app data folder (typically `C:\Users\<YourUsername>\AppData\Local`)
- `YYYYMMDD` is the current date (e.g., `app_20250130.log`)

Each log entry includes:
- Timestamp with millisecond precision
- Operation details
- Error information (if applicable)
- License assignment details
- On-premises sync status
- Group membership changes
- Usage location settings

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Support

For support, please open an issue in the GitHub repository or contact the development team.

## Acknowledgments

- Built using Microsoft Graph API
- Powered by .NET 8.0