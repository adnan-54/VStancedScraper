# VStanced Scraper

## Overview
VStanced Scraper is a console application written in C# designed to download all publicly available pages from vstanced.com. As the site is shutting down, this tool aims to preserve valuable content dating back to 2010, celebrating the memories and moments spent there.

## Features
- Downloads content from all pages, forums, downloads, and user profiles.
- Supports downloading sent and received private messages with credentials.

## Constants in `Common.cs`
- **PHPSESSID**: The PHP session stored in cookies when logged in.
- **ctf9d32bcc1039e927**: Your authentication token stored in cookies when logged in.
- **BASE_PATH**: The output folder for downloaded content.
- **MAX_CONCURRENT_REQUESTS**: The maximum requests that can be done in parallel (recommended: 5).

## Building and Running the Application

### Using Visual Studio
1. Open the solution file (`.sln`) in Visual Studio.
2. Restore NuGet packages if prompted.
3. Build the solution (Build > Build Solution).
4. Run the application (Debug > Start Without Debugging or press Ctrl + F5).

### Using .NET CLI
1. Open your terminal and navigate to the project directory.
2. Restore dependencies:
	```bash
   dotnet restore
	```
3. Build the project:
	```bash
   dotnet build
	```
4. Run the application:
	```bash
   dotnet run --project VStancedScraper
	```

## License
This project is licensed under the BSD 3-Clause License. See the [LICENSE](LICENSE) file for more information.

## Acknowledgements
Thank you to the VStanced community for the memories and moments shared over the years. This project is dedicated to all members, past and present, who contributed to the community.
