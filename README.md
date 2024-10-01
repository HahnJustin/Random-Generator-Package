# Random Generator Package (Not a full release)

A Unity package for generating random maps. This tool is designed to create hundreds of randomly generated maps for games with async calls and 

## Features

- **Multithreaded Map Generation:** Generate maps using multiple threads to optimize performance for large-scale map generation.
- **Configurable Parameters:** Customize map size, tile types, and seed for generation to create diverse environments.
- **Serializable Parameter Objects:** Can save paramaters in an object to use later
- **Dynamic Algorithm Stack:** Customize which algorithms you want to run in which order
- **Visualizer Compatibility:** Works with a random generator visualizer that will eventually export parameter files for use in unity projects, this visualizer also has a built-in testing framework
- **Texture Packing:** Support for packing multiple tilemaps into textures for efficient rendering.
- **Event System:** Support for events that run on different outcomes of the generation, such as finish or error

## Installation

1. Clone this repository or download the zip file.
2. Add the package to your Unity project by copying the `RandomGenerator` folder into your `Packages` directory.
3. Open Unity and go to **Assets > Import Package > Custom Package** to import it into your project.

## Getting Started

1. Add the `GenerationManager` component to any GameObject in your scene.
2. Set your map dimensions, stack of generator algorithms, tile database objects, and layer database objects in the Inspector (The databases come with a default tileset and layer info set)
3. Make a tilemap creator object (that has grid component as well) this will instantiate the tilemaps as children if not predefined
4. Call `Generate()` from a script or set it to auto-generate in the editor.

### Example Usage

```csharp
using UnityEngine;
using RandomMapGenerator;

public class Example : MonoBehaviour
{
    public GenerationManager generationManager;

    void Start()
    {
        generationManager.Generate();
    }
}
