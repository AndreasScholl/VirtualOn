# Virtual On model importer
Import 3d character models and animations from Sega Saturn game Virtual On

# Usage and info
- Open the project in Unity with the SampleScene
- Select the import object in the scene and setup your import parameters 
  (mainly the path to your game files)
- Start game in unity editor and wait for import to be finished
- Export models by right clicking the object in the hierarchy and selecting "Export to FBX..."
- Textures will be outputed to the Textures folder (from project root)
- A simple uv-mapped texture atlas named "TextureMap.png" will be created for each character 

# Known problems
- Some shoulder offsets are incorrect and have to be adjusted manually after the import.
- Fei Yens attachments (hair and skirt) get not applied
- Vipers rig gets not built up correctly
- Transparancy / alpha channel is not handled
