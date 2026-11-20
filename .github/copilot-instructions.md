# GitHub Copilot Instructions for A Petition for Provisions (Continued)

## Mod Overview and Purpose

A Petition for Provisions (Continued) is a RimWorld mod that allows players to request items from non-hostile factions. The mod aims to enhance the game's economy and diplomacy by enabling players to make specific item requests, which the factions will then fulfill by sending caravans. The mod provides a more controlled and strategic approach to managing resources and relationships with factions.

## Key Features and Systems

- **Item Requests:** Players can request specific items from non-hostile factions through the comms console.
- **Faction Restrictions:** Players cannot request items from factions that do not have traders.
- **Caravan Management:** The mod manages the travel time and arrival of caravans based on the distance and factions' tech levels.
- **Search Bar:** A search bar is added to the trade window to help players find specific items.
- **Tooltips:** Tooltips provide information about the current petitioned caravans.
- **Language Support:** The mod includes Russian and Chinese translations.
- **Request Catalogue:** Definitions and their valid material combinations are processed incrementally without creating Things or generating pawns. Request prices use abstract market-value stats; actual items and animals are created when the caravan fulfills the request. Search results are cached until the query or filters change.

## Coding Patterns and Conventions

- **Modding Framework:** The mod uses the RimWorld modding framework, which includes XML and C# files.
- **Harmony Patching:** Harmony is used for patching existing game code to integrate new functionality.
- **XML Integration:** XML files define mod-specific content and are used for localization and incident/job definitions.
- **Code Organization:** C# files are organized into classes and methods that handle specific functionalities, such as request confirmation, caravan management, and UI extensions.

## XML Integration

The mod includes XML files for defining incident and job definitions. These files are located in the `TranslationTemplate` directory and are used for localization and defining new game events.

- **IncidentDef_RequestedCaravanArrival.xml:** Defines the incident for when a requested caravan arrives.
- **JobDef_FulfillItemRequest.xml:** Defines the job for fulfilling a specific item request.

## Harmony Patching

Harmony is used to patch existing game code to integrate new functionality. The mod patches methods such as `DetermineCaravanTravelTimeFromFaction` in `CaravanManager` to manage caravan travel times based on faction tech levels.

## Suggestions for Copilot

1. **C# Class and Method Names:** When generating C# code, use descriptive and meaningful names for classes and methods that reflect their functionalities.
2. **XML Definitions:** When generating XML content, ensure that the definitions are correctly formatted and adhere to the RimWorld XML schema.
3. **Localization:** When adding new translations, ensure that the XML files are updated with the correct translations for different languages.
4. **Harmony Patches:** When creating Harmony patches, ensure that the patches are correctly applied and do not interfere with existing game functionality.
5. **Error Handling:** When generating code, include appropriate error handling to manage potential issues during runtime.

## Contributing

If you have any issues or suggestions, please report them using the GitHub issue tracker. Contributions are welcome, and feel free to submit pull requests with improvements or new features.

## References

- [RimWorld Modding Guide](https://rimworldwiki.com/wiki/Modding)
- [Harmony Documentation](https://harmony.pardeike.net/)

---

By following these guidelines, you can contribute effectively to the A Petition for Provisions (Continued) mod and help enhance the RimWorld experience for players.

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
- When mentioning SonarQube issues, automatically use the SonarQube MCP service to fetch and address issues instead of making inferred fixes without querying SonarQube first.
- When mentioning the rimworld log, automatically use the Rimworld MCP service to fetch the log.

