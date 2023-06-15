# dg-time_recorder
This is a simple program that records the amount of time that any other application has been running for. It can record specific application's windows by their title and records other things like for how long the app/window has been minimized or like for how long it has been receiving inputs from a specific type of device while having a/being the focused window.
## How to use
This is the first program that i have ever made, that plus the fact that i made it for my personal use in mind may not make it a very feature complete or user-friendly program, but it is pretty straight forward to use as long as you don't mess with the data files, __a content line should not have any extra blank-spaces and the file should always have an extra blank line as the last one with no blank-spaces or the program won't start.__ I feel than that should be expected, but i feel that i have to say it anyways.

You can add an element with the __"Add Program"__ button. Filling the window should be mostly straight forward, however, i would like to highlight some things: 
- **Name** - Is any desired name to show in the UI. 
- **Starting Hours** - Is only the number of hours that the element will be added with. 
- **Record a Window by Title** is pretty self-explanatory, **--however--**, there are some details about how it works exactly: it matches letter cases always. It will try to find if the window title contains that exact sequence of characters somewhere, unless if **"Match Exactly"** is checked, in that case it will check if the text is the exact same as the window title.
- **Use specific icon on list** allows you to use an image file or the icon of a file as the icon shown in the UI.
- **Use first recorded window's icon** - This will show the icon of the first window that this element records on the UI permanently. This icon will be saved inside the __"icons"__ folder.

The last two options pretty much say what they are for, however, the last one might be the most confusing one. This gets used when the number of seconds specified on the box above is surpassed, this will be the number of seconds saved right after the last recorded input.

The program shows the elements data on the UI list, the ones that need more explanation are:

- **Minimized** - The number of hours that all the windows matching this element have been simultaneously minimized for.
- **+Inputs** - The number of hours that a focused window matching this element has been receiving any kind of device inputs for.
- **K**, **M** and **C** - The number of hours that a focused window matching this element has been receiving keyboard, mouse or controller inputs respectively.
- **K+M** - The number of hours that a focused window matching this element has been receiving either keyboard or mouse inputs.

The only way to **change of position**, **modify** or **delete elements from the list** is by doing it directly from the __"processlist"__ file on the __"data"__ folder. You can only do any of this while the program is closed to avoid losing data and by doing this, you have to make sure that the file is structured correctly like said at the start or the program will not start.

Data order:

1. **Enabled** - Unused. (0/1).
2. **Record Window** - (0/1).
3. **Match Mode** - (0: Find Sequence/1: Match Exactly).
4. **Name**.
5. **Process Name** - Exact process name. (Executable name + .exe).
6. **Window Text Match** - The text to try matching when Record Window is activated.
7. **Program's Path** - Program's full path (with executable name + .exe).
8. **Icon's Path** - Path of the image/file to get the ui icon from (Leave empty to use .exe one).
9. **Total Hours**.
10. **Minimized Hours**.
11. **Focused Hours**.
12. **+Input Hours**.
13. **Keyboard Hours**.
14. **Mouse Hours**.
15. **Keyboard+Mouse Hours**.
16. **Controller Hours**.
17. **Record seconds after last input**.
18. **Save seconds after surpassing 'record seconds'**.
19. **First recorded date**.
20. **Last recorded date**.

The hours are actually saved has miliseconds on the file, so if you want to know how much is it or want to add your own values you will have to make the conversion.

If you want an already added element to use the icon of a window, you can add **waitWnd** into the data position **"Icon's Path"** (.6) and it will use the icon of the first window that it records next. If you want to use the application's .exe icon just let it empty. While adding a file's path for an icon, you can use files straight from the recorder program's directory by adding __".\\"__ at the path start, this will tell the program to use the current program's directory. This is done automatically if you add an element through the __"Add Program"__ button.

The __"settings"__ file has only one value, it represents a __custom controller joystick deadzone threshold__, the value is summed with Windows own deadzone threshold. If the threshold is surpassed it will start recording joystick movement inputs. I used this for a controller emulator that didn't centered correctly. The default value of 50 is pretty low, if you will use this program with a controller make sure it only records it when you are actually moving your joysticks, if you care about that. 

## Other issues, details and limitations
- **The program can't differentiate between elements with the same process name but than have different paths**, this is because the program's path is only utilized for the element icon on the UI at the moment. This wasn't the case while making the program, but later i found out that a program without a Microsoft certificate or admin privileges can't get the path of another that has elevated privileges. I could make it so if a process path cannot be obtained it just ignores checking the path and make it only check the process name, that way making anyone who wants to distinguish between elements that have the same process name but different paths able to, that way making this capable of getting the elevated privileges programs paths if this is run with admin privileges as well. I didn't do this because it was more work and i saw it as an inconvenience with low probability of being an issue, and i didn't wanted to force anyone to run this program with admin privileges.

- Because of the same issue described above, receiving devices inputs that aren't from a controller wasn't really easily achievable, the only way to get the inputs on an elevated privileges process while this one isn't is to check the status of the individual virtual-keys, this only works for buttons, that means that some mouse inputs are an issue, **detecting mouse wheel movements globally are straight up impossible without hooks, and for detecting mouse movements, it gets the current cursor position and compares it with the last one, this makes it a bit unreliable on videogames because some of them try to keep the cursor at the same position, and there might be some out there that may stop it completely**, but i haven't found one yet. This could be improved with the use of hooks and the same ideas as described before, but i didn't because of the same reasons.

- **The minimized time only starts getting recorded when all the windows that match the element are minimized**, this can be an issue because there could be more windows on a process that the ones you can interact with or see, these windows will always be considered non-minimized, a filter is used to determinate which windows should be considered possible to be minimized, but idk if it works on all cases. If you are having issues trying to record a specific window, try using a more specific text.

- **A window's inputs only get recorded if it is the foreground window**, that means that even if a window is receiving inputs, they don't get recorded unless it is the foreground window.

- **The program only checks if a process starts or stops once every second**, making it check more often increases CPU usage.

- Temp and unused files from the "icons" and "data" folders gets deleted each time the program starts.

The code is kinda messy, so if you are thinking about checking it out for anything good luck lol <br />
Credits.txt has some links to posts and people yt videos that helped me to make this program.
