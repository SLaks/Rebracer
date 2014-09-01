#Rebracer

Rebracer solves an age-old problem with working on C# projects from a variety of source: source formatting settings.
If you work on different open source projects, or on projects for different companies, each one is likely to have a different One True Brace Style, forcing you to change your Visual Studio settings every time you switch projects.

Rebracer solves this by storing these settings alongside each solution.  When you open a solution, Rebracer will automatically apply that solution's settings, leaving you free to write code as usual.

##How it works
Whenever you open a solution, Rebracer will check for a `Rebracer.xml` file in the solution directory.  If it finds one, it will load all settings from that file so that the solution will use its specified settings.  When you change settings in Visual Studio's regular Options dialog, Rebracer will update the XML file with the changed settings. 

To use Rebracer for a new solution, right-click the solution, click Add, New Rebracer Settings File.  This will create a Rebracer.xml for the solution, seeded with your current VS settings.  You can then open the Options dialog to choose settings specific to that solution.  From then on, anyone who opens that solution with Rebracer installed will get those settings.  You can also copy an existing Rebracer.xml file from a different solution to start with its settings.  If so, you must close and re-open the solution to pick up the new file.

##FAQ
 - Q: I want to contribute to a project that uses Rebracer.  What do I need to do?  
   A: Just install Rebracer from the Visual Studio Extension Gallery.  That's it &ndash; you don't need to do anything else.

 - Q: Should I put Rebracer.xml in source control?  
   A: Yes!  The whole point of Rebracer is that people who check out your solution from source controll will get the same settings.  

 - Q: Will this generate merge conflicts as people change settings?  
   A: Rebracer maintains its settings files in a consistent manner (always sorted, ignoring extra properties) to avoid merge conflicts.

 - Q: What about tabs vs. spaces?  
   A: Use the existing [EditorConfig extension](http://visualstudiogallery.msdn.microsoft.com/c8bccfe2-650c-4b42-bc5c-845e21f96328), which allows you to specify newline and whitespace settings for individual files or patterns, and works with most editors.  
    If you want to maintain a single settings file, you can manually add `<ToolsOptionsSubCategory name="CSharp">` under `<ToolsOptionsCategory name="TextEditor">` in Rebracer.xml:
   ```
  <ToolsOptionsSubCategory name="CSharp">
    <PropertyValue name="InsertTabs">true</PropertyValue>
  </ToolsOptionsSubCategory name="CSharp">
  ```

 - Q: Will this mess up my global settings for projects that don't use Rebracer?  
   A: Nope!  Rebracer maintains a separate XML file in your Visual Studio profile containing your original global settings.  If you open or create a solution that doesn't have a Rebracer settings file, it will revert to your existing global settings.

 - Q: What settings are supported?  
   A: Any settings saved in the `<ToolsOptions>` element of a VSSettings file (the [`DTE.Properties` collections](http://msdn.microsoft.com/en-us/library/ms165641.aspx) in extensibility).  MSDN has a [list of predefined categories](http://msdn.microsoft.com/en-us/library/ms165641.aspx).  
     By default, Rebracer settings files only contain settings that are useful to roam with projects.  If you edit the file manually, you can add other categories and they will be serialized too.

 - Q: Isn't this a security risk? What if I open a hostile project that includes a malicious settings file?  
   A: Rebracer will refuse to apply any settings that have security implications, and will print a warning message in the Output window if such settings are encountered.  See [KnownSettings.cs](Rebracer/Utilities/KnownSettings.cs) for the list of dangerous settings.

 - Q: Why is the XML file so verbose?  
   A: The XML file was designed to mirror Visual Studio's standard VSSettings files, which store user settings and are used by the Import/Export Settings wizard.  Note that I have not actually tested this compatibility.

 - Q: Can I have different settings for different projects in a solution?  
   A: No; sorry.  (switching settings as you switch projects is far more complicated, and cannot work at all for non-editor-specific settings.)  If you want to change newline or indentation settings on a per-project or even per-file basis, use [EditorConfig](http://editorconfig.org/).

 - Q: Can this work with MonoDevelop?  
   A: Great question! I have no idea.  
     Adding such compatibility would require a separate version of this extension for MonoDevelop, which would need to map property names from Visual Studio.
