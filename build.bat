@echo off
REM CBAHI Ambulatory Care Portal - Build Script

echo ========================================
echo CBAHI Portal - Build Script
echo ========================================
echo.

REM Check if dotnet CLI is installed
dotnet --version >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET CLI is not installed or not in PATH
    exit /b 1
)

echo Step 1: Cleaning previous builds...
dotnet clean
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Clean failed
    exit /b 1
)
echo Clean completed successfully.
echo.

echo Step 2: Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Restore failed
    exit /b 1
)
echo Restore completed successfully.
echo.

echo Step 3: Building projects...
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed
    exit /b 1
)
echo Build completed successfully.
echo.

echo Step 4: Running tests...
dotnet test --configuration Release --verbosity normal
if %ERRORLEVEL% NEQ 0 (
    echo WARNING: Tests failed (but build succeeded)
) else (
    echo Tests completed successfully.
)
echo.

echo ========================================
echo BUILD SUCCESSFUL!
echo ========================================
echo.
echo Next steps:
echo 1. Configure appsettings.json with your database connection
echo 2. Run: cd src\AmbulatoryCarePortal.Presentation
echo 3. Apply migrations: dotnet ef database update --project ..\AmbulatoryCarePortal.Infrastructure
echo 4. Start the application: dotnet run
echo.
pause
