# Dressmaker-ScissorsReplacer
Codebase for my scissors replacer mod for Dressmaker


Please note that all code is provided as is; this code base is intended as a reference point for folks with some experience coding already to see how I'm referencing objects in Dressmaker for my mods like my scissors replacer and mannequin replacer mods.


OBJImporter comes from the OBJImporter library package on the Unity Asset Store - https://assetstore.unity.com/packages/tools/modeling/runtime-obj-importer-49547

Modifications were made so texture/material loading could be done seperately. I intend to pull out specifically the bits creating meshes in the hopes of changing this library to load int oa prexisting GameObject instead of only being able to create new ones.


If you would like to reference, copy, or reuse my code, you are 100% allowed to so long as you are not just dumping it into an LLM agent to vibe code for you. 
You need to edit the project file to update the files paths:
DressmakerGameRoot = needs to be the folder your Dressmaker.exe is in
OutputPath = make a folder in your plugins folder and put the file path here. Visual Studio will automatically output the build to this location so you don't have to move it from the bin constantly



Stable builds of this mod are uploaded to Nexus Mods here: https://www.nexusmods.com/dressmaker/mods/1284?tab=description