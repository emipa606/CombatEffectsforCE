# .github/copilot-instructions.md

## Mod Overview and Purpose

### Mod Name: Combat Effects for Combat Extended (Continued)

This mod is an extension for the popular RimWorld mod "Combat Extended" (CE). It enhances the combat visuals by adding various effects, including sparks for bullet impacts and significant blood effects on pawn hits. The intent is to bring more realism and immersion to combat scenarios within RimWorld. 

The mod provides additional depth to gameplay by allowing bullets to penetrate materials based on caliber, ammo type, and material condition, enhancing the strategic elements of combat setups. This mod is designed to be loaded after the Combat Extended mod for full functionality.

## Key Features and Systems

### Major Features:
- **Visual Effects:** Enhanced visual effects such as sparks for bullet impacts on hard surfaces and blood effects on pawns.
- **Bullet Penetration:** Bullets can penetrate walls and pawns based on multiple factors like caliber and material integrity.
- **Customizable Effects:** Options to enable or disable specific effects like extra blood-spatter.
- **Compatibility:** Tailored for use with "Combat Extended" and requires patching for additional ammo defs.
- **Projectile Mechanics:** Randomized penetration chance and projectile traversal logic (e.g., bullets traveling through multiple pawns).

### System Implementations:
- **Penetration Mechanics:** Different bullets have varying chances to penetrate materials based on ammo type and the condition of the structure.
- **Kinetic Energy and Angle of Attack:** Planned features to further develop realistic projectile physics.

## Coding Patterns and Conventions

### C# Development:
- **Namespaces** and **Classes:** Ensure namespaces mirror the mod structure and maintain clear class definitions for all components.
- **Access Modifiers:** Internal access for mod-specific classes (`internal class`) and public access where cross-mod interactions are necessary.
- **Method Practices:** Use descriptive naming conventions (e.g., `LogImpact`, `ChangeGraphicColor`) to facilitate code readability and maintenance.
- **Code Comments:** Provide comments for complex logic, particularly around impact calculations and graphic modifications.

### Project Structure:
- **Mod Settings and Initialization:** Static utility classes (e.g., `My_CE_Utility`) and initialization functions in `Main` class to organize initialization workflows.
- **Extensions and Utilities:** Static helper classes like `ImpactHelper` are used to encapsulate reusable methods.

## XML Integration

- XML configurations should handle all aspects related to graphics and texture definitions. 
- Ensure all XML def types (especially those for ammo) are clearly documented and defined in separate .xml files.
- XML integration with Harmony patches facilitates dynamic changes without altering base game files directly.

## Harmony Patching

- **Method Patch Types:** Use Harmony patches to modify or extend methods in the original Combat Extended mod.
- **Patch Priorities:** Ensure that patches are loaded in require order to mitigate conflicts. Critical patches should be prioritized over others.
- **Validation:** Thorough testing of patches is necessary to prevent NaN errors and ensure projectile logic consistency.

## Suggestions for Copilot

**Feature Recommendations for Copilot:**
- **Assist with Harmony Patches:** Generate boilerplate code for common Harmony patch patterns.
- **Automatic XML Reflection:** Mirror XML changes in C# code through automated suggestion of def class generation.
- **Projectile System Enhancements:** Provide algorithm suggestions for complex physics calculations like kinetic energy loss and angle of attack adjustments.
- **Debugging Assistance:** Suggest error handling techniques when working with high-complexity logic such as projectile impact and penetration calculations.

By adhering to these structured coding practices and configurations while leveraging GitHub Copilot suggestions, development for RimWorld modding can become more efficient and maintainable.
