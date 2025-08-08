# GitHub Copilot Instructions for "A Petition for Provisions (Continued)" RimWorld Mod

## Mod Overview and Purpose

**Mod Name:** A Petition for Provisions (Continued)

This mod allows players to request items from non-hostile factions in RimWorld, thus enabling the ability for players to be on the requesting side and create a more dynamic interaction with factions. It offers a unique perspective where other factions endure the hardships of travel over long distances to deliver requested items to your colony. This enriches the gameplay experience by balancing faction requests and converting them into negotiations where the player's colony stands to benefit.

## Key Features and Systems

- Fixes to short-hash errors and infinite caravan bugs.
- Improvements in the accuracy of available items based on faction tech levels.
- Enhanced GUIs in the trade-window, including tooltips and a search bar.
- Ensures factions without traders cannot be requested from.
- Incorporates Russian and updated Chinese translations.
- Allows for a trade request system through the comms console.

## Coding Patterns and Conventions

- **Static Classes:** Used for utility classes like `FloatMenuMakerMap_AddHumanlikeOrders` for creating menu items.
- **Public and Internal Classes:** Public classes, like `CaravanManager`, are used for core functionalities accessible across modules, while internal classes, such as `FulfillItemRequestWindow`, are used for implementation details.
- **Method Naming:** Follow C# conventions with PascalCase for methods, e.g., `ComputeTotal` and `UpdateColonyCurrency`.

## XML Integration

XML is utilized for defining static data such as item definitions and faction properties. Ensure XML files complement C# logic by reflecting accurate data structures and defaults.

## Harmony Patching

Harmony patches are used to inject or modify methods within RimWorld’s base game DLLs. This allows the mod to seamlessly extend or modify existing game behavior, such as adjusting caravan request logic without directly altering decompiled source files.

## Suggestions for Copilot

### General Guidance
- **Autocomplete method stubs:** When writing new methods, Copilot can propose method signatures and parameter suggestions based on existing methods.
- **Suggest logic flow:** For implementing user interface improvements or additional features, Copilot can assist with control flow and UI bindings, utilizing existing class and method patterns.
- **XML Suggestions:** Propose XML templates for new data types or configuration settings within the mod context.

### Specific Suggestions
1. **Enhancing GUIs:**
   - Suggest rendering logic and layout organization for trade windows using Rects and Element drawing functions, following the examples in `ItemRequestWindow`.

2. **Efficiency Improvements:**
   - Recommend refactors to loops or condition checks, ensuring operations like `FilterRequestableItems` remain performant as the dataset grows.

3. **Patch Integration:**
   - Harmonize additional method patches by suggesting common method prototypes and patch points, using existing harmony constructs found within the mod.

4. **Translations:**
   - Suggest placeholders and integration methods for additional languages using the existing framework set by Russian and Chinese translations.

By following these structured instructions, contributors can effectively utilize GitHub Copilot to maintain and extend the functionality of "A Petition for Provisions (Continued)" ensuring a robust and enjoyable player experience.
