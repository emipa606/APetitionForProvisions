# GitHub Copilot Instructions for A Petition for Provisions (Continued)

## Mod Overview and Purpose

**Mod Name:** A Petition for Provisions (Continued)  
**Author:** ToyWalrus  
**Description:** This mod allows players of RimWorld to request specific items from non-hostile factions, flipping the script on traditional caravan mechanics. Instead of risking your colonists to fulfill faction requests, you can now demand goods from others. The mod ensures that the cost of items is adjusted based on the distance and tech level of the factions, introducing strategic balance to item requests. The overall goal is to enhance gameplay by offering more interactive and valuable interactions with NPC factions.

## Key Features and Systems

- **Item Requests:** Players can request items from friendly factions using the comms console. Each faction can have one active request at a time.
- **Dynamic Pricing:** Item costs are influenced by distance from the player's base and the tech level of the faction, promoting strategic planning.
- **Improved Trade Interface:** A search bar and tooltips enhance usability in the trade window.
- **Technical Corrections:** Fixed issues like short-hash errors, infinite caravan bugs, and key bindings in the trade window.

## Coding Patterns and Conventions

- **C# Structure:** The source code is divided into classes and methods that handle window contents, caravan management, cost calculations, and UI interactions efficiently.
- **Clean Code Practices:** Naming conventions follow CamelCase for methods and properties. Class names are descriptive and relate to their functionality, such as `ConfirmRequestWindow` and `CaravanManager`.
- **Single-Responsibility Principle:** Each class is focused on a specific aspect of the mod, fostering maintainability.

## XML Integration

- **XML Files:** 
  - **About.xml:** Contains metadata about the mod.
  - **IncidentDef_RequestedCaravanArrival.xml** & **JobDef_FulfillItemRequest.xml:** Define events and jobs related to item requests and fulfillments.
- **Localization:** Supports multiple languages, including Russian and Chinese, enhancing accessibility.

## Harmony Patching

- **Harmony Dependency:** Utilizes Harmony to patch core RimWorld methods without modifying the game's original files directly, ensuring compatibility with updates and other mods.
- **Focus on Compatibility:** Harmony is used to bridge any gaps between the base game logic and mod mechanics, enhancing the reliability of requested feature integrations.

## Suggestions for Copilot

When generating code snippets related to this mod, consider the following suggestions:

1. **UI Enhancements:** Consider auto-suggesting methods for improving user interface elements like tooltips and search functions within trade and item request windows.
2. **Localization Support:** Generate code templates that support adding new language translations smoothly.
3. **Modularity:** Provide patterns for breaking down complex methods into smaller, more manageable pieces in accordance to Single Responsibility Principle.
4. **Performance Optimization:** Suggest caching strategies for travel time calculations in the `CaravanManager` to reduce redundant calculations.
5. **Error Handling:** Emphasize robust error handling in code related to trade requests and interactions with other mods to prevent unexpected crashes or conflicts.

These instructions aim to maintain consistency, improve the mod's scalability, and assist in achieving a seamless player experience while using GitHub Copilot.

## Project Solution Guidelines
- Relevant mod XML files are included as Solution Items under the solution folder named XML, these can be read and modified from within the solution.
- Use these in-solution XML files as the primary files for reference and modification.
- The `.github/copilot-instructions.md` file is included in the solution under the `.github` solution folder, so it should be read/modified from within the solution instead of using paths outside the solution. Update this file once only, as it and the parent-path solution reference point to the same file in this workspace.
- When making functional changes in this mod, ensure the documented features stay in sync with implementation; use the in-solution `.github` copy as the primary file.
- In the solution is also a project called Assembly-CSharp, containing a read-only version of the decompiled game source, for reference and debugging purposes.
- For any new documentation, update this copilot-instructions.md file rather than creating separate documentation files.


## Hard rules (must follow)
- Do NOT run commands that modify the repo (no git commit, git apply, dotnet format) unless explicitly asked.
- Prefer minimal reads: read only the smallest code region needed (around the suspicious lines).

