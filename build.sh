#!/bin/bash

# CBAHI Ambulatory Care Portal - Build Script for Linux/Mac

echo "========================================"
echo "CBAHI Portal - Build Script"
echo "========================================"
echo ""

# Check if dotnet CLI is installed
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET CLI is not installed or not in PATH"
    exit 1
fi

echo "Step 1: Cleaning previous builds..."
dotnet clean
if [ $? -ne 0 ]; then
    echo "ERROR: Clean failed"
    exit 1
fi
echo "Clean completed successfully."
echo ""

echo "Step 2: Restoring NuGet packages..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "ERROR: Restore failed"
    exit 1
fi
echo "Restore completed successfully."
echo ""

echo "Step 3: Building projects..."
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "ERROR: Build failed"
    exit 1
fi
echo "Build completed successfully."
echo ""

echo "Step 4: Running tests..."
dotnet test --configuration Release --verbosity normal
if [ $? -ne 0 ]; then
    echo "WARNING: Tests failed (but build succeeded)"
else
    echo "Tests completed successfully."
fi
echo ""

echo "========================================"
echo "BUILD SUCCESSFUL!"
echo "========================================"
echo ""
echo "Next steps:"
echo "1. Configure appsettings.json with your database connection"
echo "2. Run: cd src/AmbulatoryCarePortal.Presentation"
echo "3. Apply migrations: dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure"
echo "4. Start the application: dotnet run"
echo ""
