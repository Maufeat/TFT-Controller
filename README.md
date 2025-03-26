# TFT Controller

TFT Controller is a companion application designed for players who want to use a controller with Teamfight Tactics on Windows.

## Controls

*Note: The default mapping assumes that you have set the corresponding keybindings in TFT. You can customize these mappings by editing the `Utilities/InputSimulator.cs` file.*

- **Left Joystick:** Free cursor movement.
- **Left D-Pad:** Snap navigation through the UI layout.
- **A Button:** Simulates a mouse click or drag.  
  - On the board, bench, or item areas, a single press automatically drags the champion (no holding needed).
  - In the shop, augment screens, or during free cursor movement, it performs a simple click.
- **X Button:** Reroll.
- **Y Button:** Buy XP.
- **LB Button:** Sell the hovered champion *(subject to change)*.

## Setup

1. **Clone the Repository:**  
2. **Open in Visual Studio:**
  Ensure you have the required C# tools installed.
3. **Compile the Project.**
4. **Run the Overlay:**
Launch the overlay while TFT is running (game detection is in progress).
