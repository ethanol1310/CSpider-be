.PHONY: help format clean-usings check-format verify

# Find the solution file
SOLUTION_FILE := $(shell find . -maxdepth 1 -name "*.sln")

# Default target when just running 'make'
help:
	@echo "Available targets:"
	@echo "  format        - Format all C# files and remove redundant usings"
	@echo "  clean-usings  - Remove redundant using directives"
	@echo "  check-format  - Check if files need formatting (returns non-zero if formatting needed)"
	@echo "  verify       - Run both format check and clean-usings check"
	@echo "\nUsing solution file: $(SOLUTION_FILE)"

# Format all C# files and remove redundant usings
format:
	@echo "Formatting C# files..."
	@dotnet format "$(SOLUTION_FILE)" --verify-no-changes false
	@echo "Removing redundant usings..."
	@dotnet format "$(SOLUTION_FILE)" whitespace --verify-no-changes false
	@dotnet format "$(SOLUTION_FILE)" style --verify-no-changes false
	@echo "Done!"

# Remove redundant using directives
clean-usings:
	@echo "Removing redundant using directives..."
	@dotnet format "$(SOLUTION_FILE)" style --include-style IDE0005 --verify-no-changes false
	@echo "Done!"

# Check if any files need formatting
check-format:
	@echo "Checking if files need formatting..."
	@dotnet format "$(SOLUTION_FILE)" --verify-no-changes

# Verify both formatting and usings
verify:
	@echo "Verifying code format and usings..."
	@dotnet format "$(SOLUTION_FILE)" --verify-no-changes
	@dotnet format "$(SOLUTION_FILE)" style --include-style IDE0005 --verify-no-changes