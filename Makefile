.PHONY: help format 

# Find the solution file
SOLUTION_FILE := $(shell find . -maxdepth 1 -name "*.sln")

format:
	@echo "Formatting C# files..."
	@dotnet format "$(SOLUTION_FILE)" --verify-no-changes false
	@echo "Removing redundant usings..."
	@dotnet format "$(SOLUTION_FILE)" whitespace --verify-no-changes false
	@dotnet format "$(SOLUTION_FILE)" style --verify-no-changes false
	@echo "Done!"

