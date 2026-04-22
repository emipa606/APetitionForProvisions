# .github/copilot-instructions.md

## Mod Overview and Purpose

**Mod Name:** A Petition for Provisions (Continued)  
This RimWorld mod enhances the gameplay by allowing players to request items from non-hostile factions. Instead of always fulfilling external demands, players can now initiate their own requests, urging other factions to traverse the harsh environments of the Rim to deliver desired items. This mod revamps the trade system, adding strategic depth and player agency by enabling request-based interactions with factions.

## Key Features and Systems

- **Request System:** Initiate item requests from non-hostile factions, making them deliver desired goods. 
- **Dynamic Pricing:** Item prices increase with distance, introducing strategic considerations in request planning.
- **Trade-Window Enhancements:** 
  - Integrated search functionality for easier item navigation.
  - Added tooltips detailing current petitioned caravans.
- **Bug Fixes and Improvements:** 
  - Resolved short-hash errors and infinite caravans issue.
  - Improved stuff filter functionality.
  - More accurate item availability based on faction tech levels.
  - Fixed functionality of Esc/Enter keys in the trade window.
- **Localization:** Added Russian translation and updated Chinese translation.

## Coding Patterns and Conventions

- **Class Design:** 
  - Use static classes for utility functions (e.g., `ExtensionsRect`, `ExtensionsString`).
  - Implement classes that extend base game classes for interaction with core systems, such as `Window` for UI elements.
- **Methodology:**
  - Break down large functionalities into smaller, focused methods for clarity, e.g., `ComputeTotal()` for cost calculations.
  - Use classes and methods interoperability to keep code modular and manageable.
- **Conventions:**
  - Follow C# naming conventions: PascalCase for class names and methods, camelCase for local variables.
  - Use descriptive method names for understanding purpose and functionality at a glance (e.g., `DrawAvailableColonyCurrency`).

## XML Integration

- **Defining Items and Requests:** Utilize XML files to define new item types and request behaviors. This allows seamless integration with RimWorld's existing XML-based data structures for modding.
- Ensure that XML data is loaded and read correctly using methods defined in classes such as `ThingDatabase` and `ThingEntry`.

## Harmony Patching

- Use [Harmony](https://harmony.pardeike.net/) to patch existing methods in RimWorld to seamlessly integrate the mod's functionalities without altering the original game code.
- Recommended to encapsulate patch-specific logic within dedicated classes such as `IncidentWorker_RequestCaravanArrival`.

## Suggestions for Copilot

- **Code Suggestions:**
  - Recommend method overrides for existing RimWorld functionalities as needed for integration.
  - Suggest refactoring opportunities when method bodies grow too complex.
- **Pattern Recognition:**
  - Use existing mod coding patterns to generate code that aligns with established practices.
  - Recognize XML structure and suggest integration points with C# logic.
- **Error Prevention:**
  - Suggest compile-time checks and runtime error handling, particularly around interaction with external data (e.g., XML config files).
- **Localization:**
  - Recommend format for adding new languages and updating translation files efficiently.

By adhering to these guidelines and suggestions, Copilot can provide useful and contextually relevant code completions that assist in further developing and maintaining the mod efficiently.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.
